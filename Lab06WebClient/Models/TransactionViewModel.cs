namespace Lab06WebClient.Models
{
    public class TransactionViewModel
    {
        public int Id { get; set; }

        public string From { get; set; } = "";

        public string To { get; set; } = "";

        public decimal Amount { get; set; }

        public DateTime TransferDate { get; set; }
    }
}
