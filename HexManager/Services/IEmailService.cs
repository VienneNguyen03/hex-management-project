namespace HexManager.Services;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string email, string verificationCode);
}
