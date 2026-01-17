using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace HexManager.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string email, string verificationCode)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"] ?? "Hex Manager";

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email configuration is missing. Email will not be sent.");
                return;
            }

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? smtpUsername, fromName),
                Subject = "Password Reset Verification Code",
                Body = $@"
Hello,

You have requested to reset your password for Hex Manager.

Your verification code is: <strong>{verificationCode}</strong>

This code will expire in 10 minutes.

If you did not request this, please ignore this email.

Best regards,
Hex Manager Team
",
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Verification code email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            if (ex is System.Net.Sockets.SocketException socketEx)
            {
                _logger.LogWarning("SMTP connection failed (Network unreachable). This is expected on Railway and similar platforms that block direct SMTP connections.");
                return;
            }
            
            if (ex is System.Net.Mail.SmtpException smtpEx)
            {
                _logger.LogWarning("SMTP error occurred: {Error}. This may be due to network restrictions on the hosting platform.", smtpEx.Message);
                _logger.LogWarning("Email service is disabled. Consider using SendGrid, Mailgun, or AWS SES for production email delivery.");
                return;
            }
            
            _logger.LogError(ex, "Unexpected error sending verification code email to {Email}. Error: {Error}", email, ex.Message);
            
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                throw;
            }
        }
    }
}
