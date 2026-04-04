namespace HexManager.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    string? UserEmail { get; }
    void Login(string email);
    void Logout();
}
