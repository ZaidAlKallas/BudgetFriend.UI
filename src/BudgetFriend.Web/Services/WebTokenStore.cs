using BudgetFriend.App.Application.Services;
using Microsoft.JSInterop;

namespace BudgetFriend.Web.Services;

/// <summary>
/// Persists the session in <c>localStorage</c> via interop. Read/Write
/// silently degrades to "no session" during static prerendering where
/// JavaScript is unavailable.
/// </summary>
public sealed class WebTokenStore : ITokenStore {
    private const string AccessKey = "bf.auth.access";
    private const string RefreshKey = "bf.auth.refresh";
    private const string ExpiryKey = "bf.auth.expiry";

    private readonly IJSRuntime _js;

    public WebTokenStore(IJSRuntime js) => _js = js;

    public async Task<TokenSession?> GetAsync() {
        try {
            var access = await _js.InvokeAsync<string?>("localStorage.getItem", AccessKey).ConfigureAwait(false);
            var refresh = await _js.InvokeAsync<string?>("localStorage.getItem", RefreshKey).ConfigureAwait(false);
            var expiryRaw = await _js.InvokeAsync<string?>("localStorage.getItem", ExpiryKey).ConfigureAwait(false);

            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh)) {
                return null;
            }

            var ticks = expiryRaw is not null && long.TryParse(expiryRaw, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow.Ticks;

            return new TokenSession(access, refresh, new DateTimeOffset(ticks, TimeSpan.Zero));
        }
        catch (InvalidOperationException) {
            // JavaScript interop is not available during prerendering.
            return null;
        }
    }

    public async Task SetAsync(TokenSession session) {
        try {
            await _js.InvokeVoidAsync("localStorage.setItem", AccessKey, session.AccessToken).ConfigureAwait(false);
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshKey, session.RefreshToken).ConfigureAwait(false);
            await _js.InvokeVoidAsync("localStorage.setItem", ExpiryKey, session.ExpireAtUtc.UtcTicks.ToString()).ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            // Non-fatal during prerender.
        }
    }

    public async Task ClearAsync() {
        try {
            await _js.InvokeVoidAsync("localStorage.removeItem", AccessKey).ConfigureAwait(false);
            await _js.InvokeVoidAsync("localStorage.removeItem", RefreshKey).ConfigureAwait(false);
            await _js.InvokeVoidAsync("localStorage.removeItem", ExpiryKey).ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            // Non-fatal during prerender.
        }
    }
}