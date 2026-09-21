using BudgetFriend.App.Data.Remote.ApiServices;
using BudgetFriend.App.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BudgetFriend.App.Application.Services;

public sealed record AuthResult(bool IsSuccess, string? Error = null);

/// <summary>
/// Manages authentication state for the entire application. Implements both
/// the Blazor <see cref="AuthenticationStateProvider"/> contract (so
/// <c>[Authorize]</c> and <c>&lt;AuthorizeView&gt;</c> work) and the
/// higher-level <see cref="IAuthService"/> that pages call to login, register
/// and logout.
/// </summary>
public sealed class AuthService : AuthenticationStateProvider, IAuthService {
    private readonly IAuthApi _authApi;
    private readonly ITokenStore _tokenStore;
    private readonly LocalizationService _local;
    private AuthenticationState _state = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private TokenSession? _session;
    private UserProfile? _profile;
    private const int NotLoggingOut = 0;
    private const int LoggingOut = 1;
    private int _loggingOut;

    public AuthService(IAuthApi authApi, ITokenStore tokenStore, LocalizationService local) {
        _authApi = authApi;
        _tokenStore = tokenStore;
        _local = local;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);

    /// <summary>
    /// Loads any persisted session and builds the initial authentication
    /// state. Must be called once after interactive rendering has started
    /// (typically in <c>OnAfterRenderAsync(firstRender: true)</c>) so that
    /// JS-based token stores are available on Web.
    /// </summary>
    public async Task InitializeAsync() {
        _session = await _tokenStore.GetAsync().ConfigureAwait(false);

        if (_session is not null && _session.ExpireAtUtc > DateTimeOffset.UtcNow) {
            SetAuthenticated();
        } else if (_session?.RefreshToken is not null) {
            var refreshed = await TryRefreshAsync().ConfigureAwait(false);
            if (refreshed)
                SetAuthenticated();
        }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public bool IsAuthenticated => _session is not null;

    // ---- Login / Register / Logout ------------------------------------------

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default) {
        try {
            var tokens = await _authApi.LoginAsync(new LoginRequest(email, password), ct).ConfigureAwait(false);
            await _tokenStore.SetAsync(new TokenSession(tokens.AccessToken, tokens.RefreshToken, tokens.ExpireAtUtc)).ConfigureAwait(false);
            _session = new TokenSession(tokens.AccessToken, tokens.RefreshToken, tokens.ExpireAtUtc);
            SetAuthenticated(email);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return new AuthResult(true);
        }
        catch (Data.Remote.ApiClient.ApiException ex) when (ex.StatusCode is 401 or 400) {
            return new AuthResult(false, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            return new AuthResult(false, _local["Error.Network"]);
        }
        catch (Exception) {
            return new AuthResult(false, _local["Error.Unexpected"]);
        }
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string? lastName, CancellationToken ct = default) {
        try {
            await _authApi.RegisterAsync(new RegisterRequest(email, password, firstName, lastName), ct).ConfigureAwait(false);
            return new AuthResult(true);
        }
        catch (Data.Remote.ApiClient.ApiException ex) {
            return new AuthResult(false, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            return new AuthResult(false, _local["Error.Network"]);
        }
        catch (Exception) {
            return new AuthResult(false, _local["Error.Unexpected"]);
        }
    }

    public async Task<AuthResult> LoginWithGoogleAsync(string idToken, CancellationToken ct = default) {
        try {
            var tokens = await _authApi.LoginWithGoogleAsync(new GoogleLoginRequest(idToken), ct).ConfigureAwait(false);
            await _tokenStore.SetAsync(new TokenSession(tokens.AccessToken, tokens.RefreshToken, tokens.ExpireAtUtc)).ConfigureAwait(false);
            _session = new TokenSession(tokens.AccessToken, tokens.RefreshToken, tokens.ExpireAtUtc);
            SetAuthenticated();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return new AuthResult(true);
        }
        catch (Data.Remote.ApiClient.ApiException ex) {
            return new AuthResult(false, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            return new AuthResult(false, _local["Error.Network"]);
        }
        catch (Exception) {
            return new AuthResult(false, _local["Error.Unexpected"]);
        }
    }

    public async Task LogoutAsync(CancellationToken ct = default) {
        // Re-entrancy guard: SendCoreAsync raises SessionExpired when any
        // authenticated request (including 401 responses from /auth/logout
        // itself) fails to refresh. Without a guard that raises SessionExpired
        // again, triggering another LogoutAsync, the call chain recurses and
        // floods the API with requests.
        if (Interlocked.CompareExchange(ref _loggingOut, LoggingOut, NotLoggingOut) != NotLoggingOut) {
            return;
        }

        try {
            if (_session is not null) {
                try {
                    await _authApi.LogoutAsync(ct).ConfigureAwait(false);
                }
                catch {
                    // Best effort: clear local state regardless of API failure.
                }
            }

            _session = null;
            _profile = null;
            await _tokenStore.ClearAsync().ConfigureAwait(false);
        }
        finally {
            // Volatile write so a concurrent reader observes the reset.
            Volatile.Write(ref _loggingOut, NotLoggingOut);
        }

        SetAnonymous();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    // ---- Password / Email flows --------------------------------------------

    public Task ForgotPasswordAsync(string email, CancellationToken ct = default)
        => _authApi.ForgotPasswordAsync(new ForgotPasswordRequest(email), ct);

    public Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
        => _authApi.ResetPasswordAsync(new ResetPasswordRequest(token, newPassword), ct);

    public Task VerifyEmailAsync(string token, CancellationToken ct = default)
        => _authApi.VerifyEmailAsync(new VerifyEmailRequest(token), ct);

    public Task ResendVerificationAsync(string email, CancellationToken ct = default)
        => _authApi.ResendVerificationAsync(new ResendVerificationRequest(email), ct);

    // ---- Profile ------------------------------------------------------------

    public async Task<UserProfile?> GetProfileAsync(CancellationToken ct = default) {
        if (_profile is not null)
            return _profile;
        try {
            _profile = await _authApi.GetProfileAsync(ct).ConfigureAwait(false);
            return _profile;
        }
        catch {
            return null;
        }
    }

    /// <summary>
    /// Clears the cached profile and refetches it from the API, so callers get
    /// up-to-date data (e.g. right after a successful email verification).
    /// </summary>
    public async Task<UserProfile?> RefreshProfileAsync(CancellationToken ct = default) {
        _profile = null;
        return await GetProfileAsync(ct).ConfigureAwait(false);
    }

    // ---- Internal -----------------------------------------------------------

    private async Task<bool> TryRefreshAsync() {
        if (_session?.RefreshToken is null)
            return false;

        try {
            var tokens = await _authApi.RefreshAsync(new RefreshTokenRequest(_session.RefreshToken)).ConfigureAwait(false);
            _session = new TokenSession(tokens.AccessToken, tokens.RefreshToken, tokens.ExpireAtUtc);
            await _tokenStore.SetAsync(_session).ConfigureAwait(false);
            return true;
        }
        catch {
            await _tokenStore.ClearAsync().ConfigureAwait(false);
            _session = null;
            return false;
        }
    }

    private void SetAuthenticated(string? email = null) {
        var claims = new List<Claim>();
        if (email is not null)
            claims.Add(new Claim(ClaimTypes.Email, email));

        if (_profile is not null) {
            var name = string.IsNullOrWhiteSpace(_profile.LastName)
                ? _profile.FirstName
                : $"{_profile.FirstName} {_profile.LastName}";
            claims.Add(new Claim(ClaimTypes.Name, name));
            claims.Add(new Claim(ClaimTypes.Email, _profile.Email));
        }

        // An Identity with a non-empty AuthenticationType marks the principal
        // as authenticated, which is all [Authorize] needs. Full profile
        // information is loaded lazily by pages after render.
        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        _state = new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void SetAnonymous() {
        _state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}