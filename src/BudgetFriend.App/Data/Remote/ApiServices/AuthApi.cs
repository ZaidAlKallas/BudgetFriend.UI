using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.ApiServices;

/// <summary>Authentication endpoints of the BudgetFriend.API.</summary>
public interface IAuthApi {
    Task RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserProfile> GetProfileAsync(CancellationToken ct = default);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);
    Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken ct = default);
    Task<TokenResponse> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}

public sealed class AuthApi : IAuthApi {
    private readonly ApiHttpClient _client;

    public AuthApi(ApiHttpClient client) {
        _client = client;
    }

    public Task RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => _client.PostAsync<RegisterRequest, object>(_client.Path("auth/register"), request, isAuthSensitive: true, ct);

    public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => _client.PostAsync<LoginRequest, TokenResponse>(_client.Path("auth/login"), request, isAuthSensitive: true, ct);

    public Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
        => _client.PostAsync<RefreshTokenRequest, TokenResponse>(_client.Path("auth/refresh"), request, isAuthSensitive: true, ct);

    public Task LogoutAsync(CancellationToken ct = default)
        => _client.PostAsync<object, object>(_client.Path("auth/logout"), null!, isAuthSensitive: false, ct);

    public Task<UserProfile> GetProfileAsync(CancellationToken ct = default)
        => _client.GetAsync<UserProfile>(_client.Path("auth/profile"), ct);

    public Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
        => _client.PostAsync<VerifyEmailRequest, object>(_client.Path("auth/verify-email"), request, isAuthSensitive: true, ct);

    public Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken ct = default)
        => _client.PostAsync<ResendVerificationRequest, object>(_client.Path("auth/resend-verification"), request, isAuthSensitive: true, ct);

    public Task<TokenResponse> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct = default)
        => _client.PostAsync<GoogleLoginRequest, TokenResponse>(_client.Path("auth/google"), request, isAuthSensitive: true, ct);

    public Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
        => _client.PostAsync<ForgotPasswordRequest, object>(_client.Path("auth/forgot-password"), request, isAuthSensitive: true, ct);

    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
        => _client.PostAsync<ResetPasswordRequest, object>(_client.Path("auth/reset-password"), request, isAuthSensitive: true, ct);
}