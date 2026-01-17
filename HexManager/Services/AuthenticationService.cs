using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using HexManager.Data;
using HexManager.Models;
using Microsoft.EntityFrameworkCore;

namespace HexManager.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _dbContext;
    private static readonly Dictionary<string, VerificationCodeInfo> _verificationCodes = new();
    private static readonly Dictionary<string, bool> _verifiedEmails = new();
    
    private const string AUTH_COOKIE_NAME = "HexManager.Auth";
    private const string AUTH_COOKIE_VALUE = "authenticated";

    public AuthenticationService(
        IConfiguration configuration, 
        IEmailService emailService,
        ILogger<AuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext dbContext)
    {
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username} - User not found", username);
                return false;
            }

            _logger.LogInformation("Login attempt - Username: {Username}, Provided password length: {Length}, Stored password length: {StoredLength}", 
                username, password?.Length ?? 0, user.Password?.Length ?? 0);
            
            if (user.Password != password)
            {
                _logger.LogWarning("Failed login attempt for username: {Username} - Invalid password. Expected: {Expected}, Got: {Got}", 
                    username, user.Password, password);
                return false;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                try
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(8)
                    };
                    
                    if (!httpContext.Response.HasStarted)
                    {
                        httpContext.Response.Cookies.Append(AUTH_COOKIE_NAME, AUTH_COOKIE_VALUE, cookieOptions);
                    }
                    else
                    {
                        _logger.LogWarning("Response has started, cookie will be set on next request");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting authentication cookie");
                }
            }

            _logger.LogInformation("User {Username} logged in successfully", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    public async Task<string> GenerateVerificationCodeAsync(string email)
    {
        var authorizedEmail = _configuration["Authentication:AuthorizedEmail"];
        
        if (string.IsNullOrEmpty(authorizedEmail))
        {
            _logger.LogWarning("AuthorizedEmail not configured in appsettings.json");
            throw new UnauthorizedAccessException("Password reset is not configured for this email address.");
        }

        if (!email.Equals(authorizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Unauthorized password reset attempt for email: {Email}", email);
            throw new UnauthorizedAccessException("This email address is not authorized for password reset.");
        }

        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        _verificationCodes[authorizedEmail] = new VerificationCodeInfo
        {
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await _emailService.SendVerificationCodeAsync(authorizedEmail, code);

        _logger.LogInformation("Verification code generated for authorized email: {Email}", authorizedEmail);
        return code;
    }

    public Task<bool> VerifyCodeAsync(string email, string code)
    {
        var authorizedEmail = _configuration["Authentication:AuthorizedEmail"];
        
        if (string.IsNullOrEmpty(authorizedEmail))
        {
            return Task.FromResult(false);
        }

        if (!_verificationCodes.ContainsKey(authorizedEmail))
        {
            return Task.FromResult(false);
        }

        var info = _verificationCodes[authorizedEmail];
        
        if (DateTime.UtcNow > info.ExpiresAt)
        {
            _verificationCodes.Remove(authorizedEmail);
            return Task.FromResult(false);
        }

        if (info.Code == code)
        {
            _verificationCodes.Remove(authorizedEmail);
            _verifiedEmails[authorizedEmail] = true;
            _logger.LogInformation("Verification code verified for authorized email: {Email}", authorizedEmail);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public async Task<bool> ResetPasswordAsync(string newPassword)
    {
        try
        {
            // Get authorized email from config
            var authorizedEmail = _configuration["Authentication:AuthorizedEmail"];
            if (string.IsNullOrEmpty(authorizedEmail))
            {
                _logger.LogError("AuthorizedEmail not configured");
                return false;
            }

            // Find user by email
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == authorizedEmail);

            if (user == null)
            {
                _logger.LogError("User not found for email: {Email}", authorizedEmail);
                return false;
            }

            // Update password in database
            user.Password = newPassword;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();

            // Clear verified emails
            _verifiedEmails.Clear();

            _logger.LogInformation("Password reset successfully for user: {Username}", user.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password");
            return false;
        }
    }

    public bool IsEmailVerified(string email)
    {
        return _verifiedEmails.ContainsKey(email) && _verifiedEmails[email];
    }

    public bool IsAuthenticated()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return false;
        
        // Check cookie
        var authCookie = httpContext.Request.Cookies[AUTH_COOKIE_NAME];
        return authCookie == AUTH_COOKIE_VALUE;
    }

    public void Logout()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && !httpContext.Response.HasStarted)
        {
            // Remove cookie
            httpContext.Response.Cookies.Delete(AUTH_COOKIE_NAME);
        }
        _logger.LogInformation("User logged out");
    }

    private class VerificationCodeInfo
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
