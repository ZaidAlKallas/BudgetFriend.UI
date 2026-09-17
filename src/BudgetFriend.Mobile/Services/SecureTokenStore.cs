using BudgetFriend.App.Application.Services;

namespace BudgetFriend.Mobile.Services;

/// <summary>Stores the session tokens in the platform's secure storage.</summary>
public sealed class SecureTokenStore : ITokenStore {
    private const string AccessKey = "auth.access";
    private const string RefreshKey = "auth.refresh";
    private const string ExpiryKey = "auth.expiry";

    public async Task<TokenSession?> GetAsync() {
        try {
            var access = await SecureStorage.Default.GetAsync(AccessKey).ConfigureAwait(false);
            var refresh = await SecureStorage.Default.GetAsync(RefreshKey).ConfigureAwait(false);
            var expiryRaw = await SecureStorage.Default.GetAsync(ExpiryKey).ConfigureAwait(false);

            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh)) {
                return null;
            }

            var ticks = expiryRaw is not null && long.TryParse(expiryRaw, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow.Ticks;

            return new TokenSession(access, refresh, new DateTimeOffset(ticks, TimeSpan.Zero));
        }
        catch {
            // SecureStorage can be unavailable (e.g. right after boot on some
            // devices); treat as "no session" rather than crashing.
            return null;
        }
    }

    public async Task SetAsync(TokenSession session) {
        await SecureStorage.Default.SetAsync(AccessKey, session.AccessToken).ConfigureAwait(false);
        await SecureStorage.Default.SetAsync(RefreshKey, session.RefreshToken).ConfigureAwait(false);
        await SecureStorage.Default.SetAsync(ExpiryKey, session.ExpireAtUtc.UtcTicks.ToString()).ConfigureAwait(false);
    }

    public Task ClearAsync() {
        SecureStorage.Default.Remove(AccessKey);
        SecureStorage.Default.Remove(RefreshKey);
        SecureStorage.Default.Remove(ExpiryKey);
        return Task.CompletedTask;
    }
}