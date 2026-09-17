namespace BudgetFriend.App.Application.Services;

public enum AppTheme {
    System = 0,
    Light = 1,
    Dark = 2
}

/// <summary>
/// Supplies the OS-level color scheme preference. Web reads the browser
/// <c>prefers-color-scheme</c> media query; Mobile reads MAUI's app theme.
/// </summary>
public interface ISystemThemeProvider {
    bool PrefersDark { get; }
}

/// <summary>
/// Centralized theme state. The preferred theme (light/dark/system) persists
/// across restarts through <see cref="IPreferenceStore"/>. The resolved
/// concrete theme (light/dark) is what the UI applies.
/// </summary>
public sealed class ThemeService {
    private readonly IPreferenceStore _preferences;
    private readonly ISystemThemeProvider _systemTheme;

    public ThemeService(IPreferenceStore preferences, ISystemThemeProvider systemTheme) {
        _preferences = preferences;
        _systemTheme = systemTheme;
    }

    /// <summary>Raised whenever the chosen or resolved theme changes.</summary>
    public event Action? ThemeChanged;

    public AppTheme SelectedTheme { get; private set; } = AppTheme.System;

    public bool IsDark { get; private set; }

    public async Task InitializeAsync() {
        var stored = await _preferences.GetAsync(IPreferenceStore.ThemeKey).ConfigureAwait(false);
        SelectedTheme = Parse(stored);
        Resolve();
    }

    public async Task SetThemeAsync(AppTheme theme) {
        SelectedTheme = theme;
        await _preferences.SetAsync(IPreferenceStore.ThemeKey, ((int)theme).ToString()).ConfigureAwait(false);
        Resolve();
        NotifyChanged();
    }

    /// <summary>Called when the OS-level color scheme preference changes.</summary>
    public Task ApplySystemSchemeAsync(bool isDark) {
        if (SelectedTheme != AppTheme.System) {
            return Task.CompletedTask;
        }

        Resolve();
        NotifyChanged();
        return Task.CompletedTask;
    }

    private static AppTheme Parse(string? value) => value?.Trim() switch {
        "1" => AppTheme.Light,
        "2" => AppTheme.Dark,
        _ => AppTheme.System
    };

    private void Resolve() => IsDark = SelectedTheme switch {
        AppTheme.Light => false,
        AppTheme.Dark => true,
        _ => _systemTheme.PrefersDark
    };

    private void NotifyChanged() => ThemeChanged?.Invoke();
}