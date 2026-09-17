using BudgetFriend.App.Application.Services;

namespace BudgetFriend.Web.Services;

/// <summary>
/// On the server the OS color scheme cannot be known synchronously; the
/// resolved value is corrected client-side via the <c>bf.watchSystemTheme</c>
/// interop channel (see MainLayout).
/// </summary>
public sealed class WebSystemThemeProvider : ISystemThemeProvider {
    public bool PrefersDark => false;
}