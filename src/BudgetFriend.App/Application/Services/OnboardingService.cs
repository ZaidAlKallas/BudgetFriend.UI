using BudgetFriend.App.Models;

namespace BudgetFriend.App.Application.Services;

/// <summary>
/// Describes how far a user has progressed through the guided onboarding
/// flow. Progress is derived from real user state: the profile (registration +
/// email verification), the set of created accounts/categories, and a local
/// "transactions concepts" flag. The only source of truth the service itself
/// owns is that concepts flag; everything else is read from existing services.
/// </summary>
public sealed record OnboardingState(
    bool RegistrationCompleted,
    bool AccountsCompleted,
    bool CategoriesCompleted,
    bool TransactionsCompleted,
    bool EmailVerified) {

    /// <summary>Onboarding is complete once the email is verified.</summary>
    public bool IsComplete => EmailVerified;

    /// <summary>True when the user has engaged with any part of the flow.</summary>
    public bool IsStarted => RegistrationCompleted || AccountsCompleted || CategoriesCompleted || TransactionsCompleted || EmailVerified;

    /// <summary>
    /// Derived progress: 20/40/60/80 on the way (registration, accounts,
    /// categories, concepts) and 100 once the email is verified.
    /// </summary>
    public int Percent => ComputePercent(this);

    /// <summary>Whether the given step (0-accounts, 1-categories, 2-concepts, 3-verify) is done.</summary>
    public bool IsStepDone(int step) => step switch {
        0 => AccountsCompleted,
        1 => CategoriesCompleted,
        2 => TransactionsCompleted,
        3 => EmailVerified,
        _ => false
    };

    public static int ComputePercent(OnboardingState state) {
        if (state.EmailVerified)
            return 100;
        if (!state.RegistrationCompleted)
            return 0;
        if (state.TransactionsCompleted && state.CategoriesCompleted && state.AccountsCompleted)
            return 80;
        if (state.CategoriesCompleted && state.AccountsCompleted)
            return 60;
        if (state.AccountsCompleted)
            return 40;
        return 20;
    }
}

/// <summary>
/// Loads the current <see cref="OnboardingState"/> from the auth profile,
/// account/category services and the persisted concepts flag.
/// </summary>
public sealed class OnboardingService {
    private readonly IAuthService _auth;
    private readonly IAccountService _accounts;
    private readonly ICategoryService _categories;
    private readonly IPreferenceStore _preferences;

    public OnboardingService(IAuthService auth, IAccountService accounts, ICategoryService categories, IPreferenceStore preferences) {
        _auth = auth;
        _accounts = accounts;
        _categories = categories;
        _preferences = preferences;
    }

    public async Task<OnboardingState> LoadAsync(CancellationToken ct = default) {
        var profile = await _auth.GetProfileAsync(ct).ConfigureAwait(false);
        var accounts = await _accounts.GetAccountsAsync(ct).ConfigureAwait(false);
        var categories = await _categories.GetCategoriesAsync(ct).ConfigureAwait(false);
        var conceptsFlag = await _preferences.GetAsync(IPreferenceStore.OnboardingTransactionsKey).ConfigureAwait(false);

        return new OnboardingState(
            RegistrationCompleted: profile is not null,
            AccountsCompleted: accounts.Count > 0,
            CategoriesCompleted: categories.Count > 0,
            TransactionsCompleted: ParseFlag(conceptsFlag),
            EmailVerified: profile?.IsEmailVerified == true);
    }

    public async Task<OnboardingState> MarkTransactionsCompletedAsync(bool completed = true, CancellationToken ct = default) {
        await _preferences.SetAsync(IPreferenceStore.OnboardingTransactionsKey, completed ? "1" : "0").ConfigureAwait(false);
        return await LoadAsync(ct).ConfigureAwait(false);
    }

    private static bool ParseFlag(string? value) =>
        value is { Length: > 0 } && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
}