using BudgetFriend.App.Resources.Localization;
using System.Globalization;
using System.Resources;

namespace BudgetFriend.App.Application.Services;

/// <summary>
/// Centralized access to the app's localized strings and culture. Uses .NET
/// resource infrastructure (<c>.resx</c> + <see cref="ResourceManager"/>).
/// The resource files live in <c>Resources/Localization</c>.
/// </summary>
public sealed class LocalizationService(IPreferenceStore preferences) {
    public const string English = "en";
    public const string Arabic = "ar";

    private static readonly ResourceManager Manager = new(
        typeof(AppStrings).FullName!,
        typeof(AppStrings).Assembly);

    private readonly IPreferenceStore _preferences = preferences;
    private CultureInfo _culture = CultureInfo.GetCultureInfo("en");

    /// <summary>Raised whenever the selected language changes.</summary>
    public event Action? LanguageChanged;

    public CultureInfo Culture => _culture;

    public string LanguageCode => _culture.TwoLetterISOLanguageName;

    public bool IsRtl => LanguageCode == Arabic;

    public string this[string key] => Manager.GetString(key, _culture) ?? key;

    public string this[string key, params object[] args]
        => string.Format(_culture, this[key], args);

    /// <summary>Short display name of a culture (used by small language chips).</summary>
    public string CultureDisplayName(string languageCode) => languageCode switch {
        Arabic => "العربية",
        _ => "English"
    };

    public async Task InitializeAsync() {
        var stored = await _preferences.GetAsync(IPreferenceStore.LanguageKey).ConfigureAwait(false);
        if (stored is not null) {
            var culture = CultureInfo.GetCultureInfo(
                stored.ToLowerInvariant().StartsWith("ar", StringComparison.Ordinal) ? Arabic : English);
            if (culture.Name != _culture.Name) {
                _culture = culture;
            }
        }
    }

    public async Task SetLanguage(string languageCode) {
        var normalized = languageCode.ToLowerInvariant().StartsWith("ar", StringComparison.Ordinal) ? Arabic : English;
        var culture = CultureInfo.GetCultureInfo(normalized);
        if (culture.Name == _culture.Name) {
            return;
        }

        _culture = culture;
        LanguageChanged?.Invoke();

        try {
            await _preferences.SetAsync(IPreferenceStore.LanguageKey, normalized).ConfigureAwait(false);
        }
        catch {
            // Persisting the preference is best-effort; the in-memory language is already applied.
        }
    }
}