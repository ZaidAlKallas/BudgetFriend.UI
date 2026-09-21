namespace BudgetFriend.App.Application.Services;

/// <summary>
/// A small key/value store for user preferences (language, theme, ...).
/// Implementations are platform specific: browser storage for Web,
/// MAUI Preferences for Mobile.
/// </summary>
public interface IPreferenceStore {
    public const string LanguageKey = "preferences.language";
    public const string ThemeKey = "preferences.theme";
    public const string FiltersCollapsedKey = "preferences.transactions-filters-collapsed";
    public const string OnboardingTransactionsKey = "onboarding.transactions-completed";

    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}