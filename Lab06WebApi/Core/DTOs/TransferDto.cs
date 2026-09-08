namespace Lab06WebApi.Core.DTOs
{
    public class TransferDto
    {
        public string ToAccount { get; set; } = string.Empty;

        // Backward compatibility if caller passes ToAccountId
        public string ToAccountId
        {
            get => ToAccount;
            set { if (!string.IsNullOrEmpty(value)) ToAccount = value; }
        }

        public decimal Amount { get; set; } = 0;
    }
}
