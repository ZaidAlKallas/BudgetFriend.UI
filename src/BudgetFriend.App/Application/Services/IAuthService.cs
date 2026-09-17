using BudgetFriend.App.Models;

namespace BudgetFriend.App.Application.Services;

/// <summary>High-level authentication operations used by UI pages.</summary>
public interface IAuthService {
    bool IsAuthenticated { get; }
    Task InitializeAsync();
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthResult> RegisterAsync(string email, string password, string firstName, string? lastName, CancellationToken ct = default);
    Task<AuthResult> LoginWithGoogleAsync(string idToken, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserProfile?> GetProfileAsync(CancellationToken ct = default);
    Task ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
    Task VerifyEmailAsync(string token, CancellationToken ct = default);
}