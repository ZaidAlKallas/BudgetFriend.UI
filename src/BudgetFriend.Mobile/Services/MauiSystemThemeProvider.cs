using BudgetFriend.App.Application.Services;

namespace BudgetFriend.Mobile.Services;

/// <summary>Reports the OS-level color scheme from MAUI.</summary>
public sealed class MauiSystemThemeProvider : ISystemThemeProvider {
    public bool PrefersDark => Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
}