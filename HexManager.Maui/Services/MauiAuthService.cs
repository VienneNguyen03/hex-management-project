using Microsoft.Maui.Storage;
using HexManager.Services;

namespace HexManager.Maui.Services;

public class MauiAuthService : IAuthService
{
    private const string AuthKey = "is_authenticated";
    private const string UserKey = "user_email";

    public bool IsAuthenticated => Preferences.Default.Get(AuthKey, false);
    
    public string? UserEmail => Preferences.Default.Get(UserKey, string.Empty);

    public void Login(string email)
    {
        Preferences.Default.Set(AuthKey, true);
        Preferences.Default.Set(UserKey, email);
    }

    public void Logout()
    {
        Preferences.Default.Set(AuthKey, false);
        Preferences.Default.Remove(UserKey);
    }
}
