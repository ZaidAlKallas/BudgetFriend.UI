using BudgetFriend.App.Application.Services;
using Microsoft.AspNetCore.Components;

namespace BudgetFriend.App.Components;

/// <summary>
/// Base for route-level components that render localized text. Injects the
/// <see cref="LocalizationService"/> and re-renders the component immediately
/// whenever the active language changes, so all text stays in sync.
/// </summary>
public abstract class LocalizedComponentBase : ComponentBase, IDisposable {
    [Inject]
    protected LocalizationService Local { get; set; } = default!;

    private bool _subscribed;

    // Subscribe before the first render instead of in OnInitialized, because a
    // derived page may override OnInitialized without calling base — which would
    // otherwise leave the component unsubscribed and its text stale.
    public override Task SetParametersAsync(ParameterView parameters) {
        EnsureSubscribed();
        return base.SetParametersAsync(parameters);
    }

    private void EnsureSubscribed() {
        if (!_subscribed) {
            _subscribed = true;
            Local.LanguageChanged += OnLanguageChanged;
        }
    }

    private void OnLanguageChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose() => Local.LanguageChanged -= OnLanguageChanged;
}