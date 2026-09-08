using System.Net;
using System.Net.Mail;
using Lab06WebApi.Core.DTOs;

namespace Lab06WebApi.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _mailSettings = configuration.GetSection("MailSettings").Get<MailSettings>() ?? new MailSettings();
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
                {
                    _logger.LogWarning("Email destination '{Email}' is invalid. Skipping SMTP delivery.", toEmail);
                    return;
                }

                using var message = new MailMessage
                {
                    From = new MailAddress(_mailSettings.Mail, _mailSettings.DisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmail));

                var password = (_mailSettings.Password ?? "").Replace(" ", "").Trim();
                using var client = new SmtpClient(_mailSettings.Host, _mailSettings.Port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_mailSettings.Mail, password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }
    }
}
