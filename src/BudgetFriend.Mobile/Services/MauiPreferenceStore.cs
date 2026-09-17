using BudgetFriend.App.Application.Services;

namespace BudgetFriend.Mobile.Services;

/// <summary>Preferences user settings in the MAUI local Preferences store.</summary>
public sealed class MauiPreferenceStore : IPreferenceStore {
    public Task<string?> GetAsync(string key)
        => Task.FromResult(Preferences.Default.Get<string?>(key, null));

    public Task SetAsync(string key, string value) {
        Preferences.Default.Set(key, value);
        return Task.CompletedTask;
    }
}