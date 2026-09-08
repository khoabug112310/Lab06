using System.ComponentModel.DataAnnotations;

namespace Lab06WebClient.Models
{
    public class TransferViewModel
    {
        [Required]
        public string ToAccount { get; set; } = "";
        [Range(1, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class ConfirmOtpViewModel
    {
        [Required]
        public string TransactionId { get; set; } = "";

        [Required]
        public string ToAccount { get; set; } = "";

        public string ToFullName { get; set; } = "";

        public decimal Amount { get; set; }

        public string MaskedEmail { get; set; } = "";

        [Required(ErrorMessage = "Please enter the OTP code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be exactly 6 digits")]
        public string Otp { get; set; } = "";
    }
}
