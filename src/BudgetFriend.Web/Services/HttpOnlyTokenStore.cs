using BudgetFriend.App.Application.Services;
using Microsoft.JSInterop;

namespace BudgetFriend.Web.Services;

/// <summary>
/// Request DTO for the session-cookie bridge endpoint (Program.cs).
/// </summary>
public sealed record BridgeSetRequest(string SessionId);

/// <summary>
/// Keeps authentication tokens entirely on the server via
/// <see cref="ServerSessionVault"/>. The browser only ever receives an opaque
/// httpOnly session cookie (set by a same-origin endpoint bridge), so
/// client-side script cannot read the access or refresh tokens.
/// </summary>
public sealed class HttpOnlyTokenStore : ITokenStore {
    private const string CookieName = "bf.session";

    private readonly IJSRuntime _js;
    private readonly ServerSessionVault _vault;
    private Guid? _sessionId;
    private TokenSession? _cached;

    public HttpOnlyTokenStore(IJSRuntime js, ServerSessionVault vault) {
        _js = js;
        _vault = vault;
    }

    public async Task<TokenSession?> GetAsync() {
        if (_cached is not null) {
            return _cached;
        }

        try {
            var id = await _js.InvokeAsync<string?>("bf.sessionGet").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(id) ||
                !Guid.TryParse(id, out var guid) ||
                _vault.TryGet(guid) is not { } session) {
                return null;
            }

            _sessionId = guid;
            _cached = session;
            return session;
        }
        catch (InvalidOperationException) {
            // JavaScript interop is not available during prerendering.
            return null;
        }
    }

    public async Task SetAsync(TokenSession session) {
        try {
            var id = _sessionId ?? Guid.NewGuid();
            _vault.Put(id, session);
            await _js.InvokeVoidAsync("bf.sessionSet", id.ToString()).ConfigureAwait(false);
            _sessionId = id;
            _cached = session;
        }
        catch (InvalidOperationException) {
            // Non-fatal during prerender.
        }
    }

    public async Task ClearAsync() {
        _cached = null;

        try {
            if (_sessionId is { } id) {
                _vault.Remove(id);
                _sessionId = null;
            }

            await _js.InvokeVoidAsync("bf.sessionClear").ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            // Non-fatal during prerender.
        }
    }
}