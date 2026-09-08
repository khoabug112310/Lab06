using Lab06WebApi.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Lab06WebApi.Data;
using Lab06WebApi.Core.Helper;
using Lab06WebApi.Core.Entities;
namespace Lab06WebApi.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly BankDbContext db;
        private readonly JwtHelper jwt;
        private readonly PasswordHasher<Account> hasher = new();
        public AuthService(BankDbContext db, JwtHelper jwt)
        {
            this.db = db;
            this.jwt = jwt;
        }
        public async Task<object?> LoginAsync(LoginDto dto)
        {
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == dto.AccountNumber);
            if (account == null) return null;
            var result = hasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed) return null;

            var refreshToken = jwt.GenerateRefreshToken();
            account.RefreshToken = refreshToken;
            account.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await db.SaveChangesAsync();

            return new
            {
                token = jwt.CreateToken(account),
                refreshToken = refreshToken,
                account.Id,
                account.AccountNumber,
                account.FullName,
                account.Balance
            };
        }

        public async Task<object?> RefreshTokenAsync(string refreshToken)
        {
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.RefreshToken == refreshToken);
            if (account == null || account.RefreshTokenExpiryTime == null || account.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            var newAccessToken = jwt.CreateToken(account);
            var newRefreshToken = jwt.GenerateRefreshToken();

            account.RefreshToken = newRefreshToken;
            account.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await db.SaveChangesAsync();

            return new
            {
                token = newAccessToken,
                refreshToken = newRefreshToken
            };
        }

        public async Task<bool> RevokeTokenAsync(int accountId)
        {
            var account = await db.Accounts.FindAsync(accountId);
            if (account == null) return false;

            account.RefreshToken = null;
            account.RefreshTokenExpiryTime = null;
            await db.SaveChangesAsync();
            return true;
        }
    }
}
