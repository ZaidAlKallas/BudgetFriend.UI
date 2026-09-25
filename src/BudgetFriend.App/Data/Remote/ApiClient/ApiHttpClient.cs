using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BudgetFriend.App.Data.Remote.ApiClient;

/// <summary>
/// Thin HTTP client wrapper shared by every API service. It owns the common
/// concerns: base URL, JSON serialization, bearer-token attachment, access
/// token refresh on 401, and conversion of error responses into
/// <see cref="ApiException"/>.
///
/// Refresh behaviour: when an authenticated request fails with 401 and the
/// request is not itself an authentication call, the client attempts a single
/// refresh against <c>/auth/refresh</c> and retries the original request.
/// If refresh fails, <see cref="SessionExpired"/> is raised so the UI can log
/// the user out. The refresh is single-flighted to avoid duplicate refreshes
/// when several calls fail at the same time.
/// </summary>
public sealed class ApiHttpClient {
    private readonly HttpClient _http;
    private readonly ApiSettings _settings;
    private readonly ITokenStore _tokenStore;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private Task<bool>? _inFlightRefresh;

    /// <summary>Raised when the stored session can no longer be refreshed.</summary>
    public event Func<Task>? SessionExpired;

    public ApiHttpClient(HttpClient http, ApiSettings settings, ITokenStore tokenStore) {
        _http = http;
        _settings = settings;
        _tokenStore = tokenStore;
    }

    // ---- Public helpers ----------------------------------------------------

    public Task<T> GetAsync<T>(string pathAndQuery, CancellationToken ct = default)
        => SendCoreAsync<T>(HttpMethod.Get, pathAndQuery, null, isAuthSensitive: false, ct);

    public Task<T> PostAsync<TIn, T>(string path, TIn body, bool isAuthSensitive = false, CancellationToken ct = default)
        => SendCoreAsync<T>(HttpMethod.Post, path, body, isAuthSensitive, ct);

    public Task<T> PutAsync<TIn, T>(string path, TIn body, CancellationToken ct = default)
        => SendCoreAsync<T>(HttpMethod.Put, path, body, isAuthSensitive: false, ct);

    public Task DeleteAsync(string pathAndQuery, CancellationToken ct = default)
        => SendCoreAsync<object>(HttpMethod.Delete, pathAndQuery, null, isAuthSensitive: false, ct);

    /// <summary>Builds a path such as <c>api/v1/accounts</c>.</summary>
    public string Path(string resource) => $"{_settings.BasePath}/{resource}";

    internal string ApiVersionName => _settings.Version;

    // ---- Core ---------------------------------------------------------------

    private async Task<T> SendCoreAsync<T>(
        HttpMethod method,
        string pathAndQuery,
        object? body,
        bool isAuthSensitive,
        CancellationToken ct,
        bool allowRefresh = true) {
        var response = await SendOnceAsync(method, pathAndQuery, body, isAuthSensitive, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            !isAuthSensitive &&
            allowRefresh) {
            // Only ever retry once, and never for flows that are part of
            // authentication itself (login, refresh, register, ...).
            var refreshed = await TryRefreshAsync(ct).ConfigureAwait(false);
            if (refreshed) {
                response.Dispose();
                return await SendCoreAsync<T>(method, pathAndQuery, body, isAuthSensitive, ct, allowRefresh: false).ConfigureAwait(false);
            }

            await RaiseSessionExpiredAsync().ConfigureAwait(false);
        }

        return await ReadResponseAsync<T>(response, ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string pathAndQuery,
        object? body,
        bool isAuthSensitive,
        CancellationToken ct) {
        var request = new HttpRequestMessage(method, new Uri($"{_settings.BaseUrl.TrimEnd('/')}/{pathAndQuery.TrimStart('/')}"));

        if (body is not null) {
            request.Content = JsonContent.Create(body, mediaType: new MediaTypeHeaderValue("application/json"), options: JsonSerializerOptions);
        }

        if (!isAuthSensitive) {
            var session = await _tokenStore.GetAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(session?.AccessToken)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            }
        }

        return await _http.SendAsync(request, ct).ConfigureAwait(false);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct) {
        await _refreshGate.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_inFlightRefresh is not null) {
                return await _inFlightRefresh.ConfigureAwait(false);
            }

            _inFlightRefresh = RefreshOnceAsync(ct);
            var result = await _inFlightRefresh.ConfigureAwait(false);
            _inFlightRefresh = null;
            return result;
        }
        finally {
            _refreshGate.Release();
        }
    }

    private async Task<bool> RefreshOnceAsync(CancellationToken ct) {
        var session = await _tokenStore.GetAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(session?.RefreshToken)) {
            return false;
        }

        var path = $"{_settings.BasePath}/auth/refresh";

        try {
            var response = await SendOnceAsync(HttpMethod.Post, path, new RefreshTokenRequest(session.RefreshToken), isAuthSensitive: true, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) {
                response.Dispose();
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonSerializerOptions, ct).ConfigureAwait(false);
            response.Dispose();

            if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken)) {
                return false;
            }

            await _tokenStore.SetAsync(new TokenSession(tokens.AccessToken, tokens.RefreshToken, tokens.ExpireAtUtc)).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            return false;
        }
    }

    private async Task RaiseSessionExpiredAsync() {
        if (SessionExpired is not null) {
            await SessionExpired.Invoke().ConfigureAwait(false);
        }
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct) {
        try {
            if (response.IsSuccessStatusCode) {
                if (response.StatusCode == HttpStatusCode.NoContent) {
                    return default!;
                }

                return await ReadJsonOrEmptyAsync<T>(response, ct).ConfigureAwait(false);
            }

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var problem = ApiProblem.TryParse(body);
            throw new ApiException((int)response.StatusCode, problem: problem);
        }
        finally {
            response.Dispose();
        }
    }

    private static async Task<T> ReadJsonOrEmptyAsync<T>(HttpResponseMessage response, CancellationToken ct) {
        if (response.Content.Headers.ContentLength == 0) {
            return default!;
        }

        try {
            var result = await response.Content.ReadFromJsonAsync<T>(JsonSerializerOptions, ct).ConfigureAwait(false);
            return result is null ? default! : result;
        }
        catch (JsonException) when (response.Content.Headers.ContentLength is null or 0) {
            // Server sent a success status with an empty body (e.g. 200/201
            // with no content). Not an error.
            return default!;
        }
    }

    public static JsonSerializerOptions JsonSerializerOptions { get; } = JsonDefaults.Api;
}