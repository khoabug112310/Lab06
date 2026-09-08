namespace Lab06WebApi.Core.DTOs
{
    public class RequestTransferOtpDto
    {
        public string ToAccount { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class ConfirmTransferOtpDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class PendingTransferSession
    {
        public int FromAccountId { get; set; }
        public string ToAccount { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
    }
}
