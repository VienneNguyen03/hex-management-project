namespace HexManager.Services;

public interface IAuthenticationService
{
    Task<bool> LoginAsync(string username, string password);
    Task<string> GenerateVerificationCodeAsync(string email);
    Task<bool> VerifyCodeAsync(string email, string code);
    Task<bool> ResetPasswordAsync(string newPassword);
    bool IsEmailVerified(string email);
    bool IsAuthenticated();
    void Logout();
}
