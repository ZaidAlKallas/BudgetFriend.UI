using BudgetFriend.App.Application.Services;
using Microsoft.JSInterop;

namespace BudgetFriend.Web.Services;

/// <summary>Persists user preferences in <c>localStorage</c> via interop.</summary>
public sealed class WebPreferenceStore : IPreferenceStore {
    private readonly IJSRuntime _js;

    public WebPreferenceStore(IJSRuntime js) => _js = js;

    public async Task<string?> GetAsync(string key) {
        try {
            return await _js.InvokeAsync<string?>("localStorage.getItem", key).ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            return null;
        }
    }

    public async Task SetAsync(string key, string value) {
        try {
            await _js.InvokeVoidAsync("localStorage.setItem", key, value).ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            // Non-fatal during prerender.
        }
    }
}