using Lab06WebApi.Core.DTOs;
using Lab06WebApi.Core.Entities;
using Lab06WebApi.Data;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Caching.Memory;

namespace Lab06WebApi.Core.Services
{
    public class TransferService : ITransferService
    {
        private readonly BankDbContext db;
        private readonly IMemoryCache cache;
        private readonly IEmailService emailService;
        private readonly ILogger<TransferService> logger;

        public TransferService(BankDbContext db, IMemoryCache cache, IEmailService emailService, ILogger<TransferService> logger)
        {
            this.db = db;
            this.cache = cache;
            this.emailService = emailService;
            this.logger = logger;
        }
        public async Task<List<object>> HistoryAsync(int accountId)
        {
            return await db.Transfers.Include(a => a.FromAccount).Include(a => a.ToAccount)
                .Where(a => a.FromAccountId == accountId || a.ToAccountId == accountId)
                .OrderByDescending(a => a.TransferDate)
                .Select(a => (object)new
                {
                    a.Id,
                    From = a.FromAccount.AccountNumber,
                    To = a.ToAccount.AccountNumber,
                    a.Amount,
                    a.TransferDate,
                    links = new[]
                    {
                        new
                        {
                            rel = "self",
                            href = $"/api/accounts/{accountId}/transfers/{a.Id}"
                        }
                    }
                }).ToListAsync();
        }

        public async Task<object> TransferAsync(int fromId, TransferDto dto)
        {
            var from = await db.Accounts.FindAsync(fromId);
            var to = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == dto.ToAccount);
            if(dto.Amount <= 0) throw new ArgumentException("Invalid transfer amount");
            if(from == null || to == null) throw new KeyNotFoundException("Account not found");
            if(from.Id == to.Id) throw new ArgumentException("Cannot transfer to the same account");
            if(from.Balance < dto.Amount) throw new InvalidOperationException("Insufficient balance");
            
            await using var tx = await db.Database.BeginTransactionAsync();
            from.Balance -= dto.Amount;  
            to.Balance += dto.Amount;
            db.Transfers.Add(new Transfer
            {
                FromAccountId = from.Id,
                ToAccountId = to.Id,
                Amount = dto.Amount,
                TransferDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new
            {
                message = "Transfer successful",
                from = from.AccountNumber,
                to = to.AccountNumber,
                amount = dto.Amount,
                links = new[]
                {
                    new
                    {
                        rel = "history",
                        href = $"/api/accounts/{from.Id}/transfers"
                    } 
                    
                } 
            };
        }

        public async Task<object> RequestTransferOtpAsync(int fromId, RequestTransferOtpDto dto)
        {
            var from = await db.Accounts.FindAsync(fromId);
            var to = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == dto.ToAccount);

            if (dto.Amount <= 0) throw new ArgumentException("Invalid transfer amount.");
            if (from == null || to == null) throw new KeyNotFoundException("Recipient account not found.");
            if (from.Id == to.Id) throw new ArgumentException("Cannot transfer to the same account.");
            if (from.Balance < dto.Amount) throw new InvalidOperationException("Insufficient balance to perform the transaction.");

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var transactionId = Guid.NewGuid().ToString("N");

            var session = new PendingTransferSession
            {
                FromAccountId = fromId,
                ToAccount = dto.ToAccount,
                Amount = dto.Amount,
                OtpCode = otp,
                ExpireAt = DateTime.UtcNow.AddMinutes(3)
            };

            cache.Set($"OTP_TRANSFER_{transactionId}", session, TimeSpan.FromMinutes(3));

            logger.LogInformation(">>> [TRANSFER OTP] TransactionId: {TxId}, OTP: {Otp}, From: {From}, To: {To}, Amount: {Amount}",
                transactionId, otp, from.AccountNumber, to.AccountNumber, dto.Amount);

            string emailContent = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #0d6efd;'>Digital Banking - Fund Transfer Verification</h2>
                    <p>Dear <b>{from.FullName}</b>,</p>
                    <p>A fund transfer request has been initiated from your account:</p>
                    <ul>
                        <li><b>Recipient Account:</b> {to.AccountNumber} ({to.FullName})</li>
                        <li><b>Transfer Amount:</b> {dto.Amount:N2}</li>
                        <li><b>Requested At:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                    </ul>
                    <p>Your one-time OTP verification code is:</p>
                    <div style='background: #f1f5f9; padding: 15px; border-radius: 8px; text-align: center; margin: 15px 0;'>
                        <span style='font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #0d6efd;'>{otp}</span>
                    </div>
                    <p><i>This code is valid for <b>3 minutes</b>. Never share this code with anyone.</i></p>
                </div>";

            _ = emailService.SendEmailAsync(from.Email, "Fund Transfer OTP Verification Code", emailContent);

            string maskedEmail = from.Email;
            if (maskedEmail.Contains('@'))
            {
                var parts = maskedEmail.Split('@');
                var namePart = parts[0];
                var maskedName = namePart.Length <= 3 ? namePart[0] + "***" : namePart.Substring(0, 2) + "***" + namePart[^1];
                maskedEmail = $"{maskedName}@{parts[1]}";
            }

            return new
            {
                transactionId,
                message = "OTP verification code has been sent to your email.",
                maskedEmail,
                toAccount = to.AccountNumber,
                toFullName = to.FullName,
                amount = dto.Amount
            };
        }

        public async Task<object> ConfirmTransferOtpAsync(int fromId, ConfirmTransferOtpDto dto)
        {
            string cacheKey = $"OTP_TRANSFER_{dto.TransactionId}";
            if (!cache.TryGetValue(cacheKey, out PendingTransferSession? session) || session == null)
            {
                throw new InvalidOperationException("Transaction session has expired or does not exist.");
            }

            if (session.FromAccountId != fromId)
            {
                throw new UnauthorizedAccessException("Invalid transaction for this account.");
            }

            if (session.OtpCode != dto.Otp.Trim())
            {
                throw new ArgumentException("Invalid OTP verification code.");
            }

            var from = await db.Accounts.FindAsync(fromId);
            var to = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == session.ToAccount);

            if (from == null || to == null) throw new KeyNotFoundException("Account not found.");
            if (from.Balance < session.Amount) throw new InvalidOperationException("Insufficient balance to complete the transaction.");

            await using var tx = await db.Database.BeginTransactionAsync();
            from.Balance -= session.Amount;
            to.Balance += session.Amount;

            var transfer = new Transfer
            {
                FromAccountId = from.Id,
                ToAccountId = to.Id,
                Amount = session.Amount,
                TransferDate = DateTime.UtcNow
            };
            db.Transfers.Add(transfer);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            cache.Remove(cacheKey);

            return new
            {
                message = "Transfer successful",
                from = from.AccountNumber,
                to = to.AccountNumber,
                amount = session.Amount,
                transferId = transfer.Id,
                links = new[]
                {
                    new
                    {
                        rel = "history",
                        href = $"/api/accounts/{from.Id}/transfers"
                    }
                }
            };
        }
    }
}
