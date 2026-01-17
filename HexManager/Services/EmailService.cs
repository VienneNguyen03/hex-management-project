using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

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
        // Try SendGrid first (preferred for production)
        var sendGridApiKey = _configuration["EmailSettings:SendGridApiKey"];
        if (!string.IsNullOrEmpty(sendGridApiKey))
        {
            await SendViaSendGridAsync(email, verificationCode, sendGridApiKey);
            return;
        }

        // Fallback to SMTP (for development)
        await SendViaSmtpAsync(email, verificationCode);
    }

    private async Task SendViaSendGridAsync(string email, string verificationCode, string apiKey)
    {
        try
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@hexmanager.com";
            var fromName = _configuration["EmailSettings:FromName"] ?? "Hex Manager";

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email);
            var subject = "Password Reset Verification Code";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .code {{ font-size: 24px; font-weight: bold; color: #714B67; background: #f8f9fa; padding: 15px; text-align: center; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Reset Verification Code</h2>
        <p>Hello,</p>
        <p>You have requested to reset your password for Hex Manager.</p>
        <div class=""code"">{verificationCode}</div>
        <p>This code will expire in 10 minutes.</p>
        <p>If you did not request this, please ignore this email.</p>
        <div class=""footer"">
            <p>Best regards,<br>Hex Manager Team</p>
        </div>
    </div>
</body>
</html>";

            var plainTextContent = $@"
Hello,

You have requested to reset your password for Hex Manager.

Your verification code is: {verificationCode}

This code will expire in 10 minutes.

If you did not request this, please ignore this email.

Best regards,
Hex Manager Team";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Verification code email sent via SendGrid to {Email}", email);
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid email failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                throw new Exception($"SendGrid email failed with status {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SendGrid to {Email}. Error: {Error}", email, ex.Message);
            throw;
        }
    }

    private async Task SendViaSmtpAsync(string email, string verificationCode)
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
                _logger.LogWarning("SMTP configuration is missing. Email will not be sent.");
                return;
            }

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProduction = !string.IsNullOrEmpty(environment) && environment != "Development";
            
            if (isProduction)
            {
                _logger.LogWarning("Production environment detected but SendGrid API key not configured. Email sending skipped.");
                _logger.LogWarning("Please configure SendGrid API key in EmailSettings:SendGridApiKey for production email delivery.");
                return;
            }

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                Timeout = 5000
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

            var sendTask = client.SendMailAsync(mailMessage);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            
            var completedTask = await Task.WhenAny(sendTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("SMTP send timed out after 5 seconds.");
                return;
            }
            
            await sendTask;
            _logger.LogInformation("Verification code email sent via SMTP to {Email}", email);
        }
        catch (Exception ex)
        {
            if (ex is System.Net.Sockets.SocketException)
            {
                _logger.LogWarning("SMTP connection failed (Network unreachable). This is expected on Railway and similar platforms.");
                return;
            }
            
            if (ex is System.Net.Mail.SmtpException smtpEx)
            {
                _logger.LogWarning("SMTP error occurred: {Error}. This may be due to network restrictions.", smtpEx.Message);
                return;
            }
            
            _logger.LogError(ex, "Unexpected error sending email via SMTP to {Email}. Error: {Error}", email, ex.Message);
            
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                throw;
            }
        }
    }
}
