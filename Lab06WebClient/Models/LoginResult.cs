namespace Lab06WebClient.Models
{
    public class LoginResult
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int Id { get; set; }

        public string AccountNumber { get; set; } = "";

        public string FullName { get; set; } = "";

        public decimal Balance { get; set; }
    }

    public class RefreshTokenResponse
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}
