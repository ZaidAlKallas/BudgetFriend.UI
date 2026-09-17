namespace BudgetFriend.App.Application.Services;

/// <summary>
/// Configuration for the BudgetFriend.API. Populated per host:
/// the Web host binds it from appsettings, the Mobile host sets it directly.
/// </summary>
public sealed class ApiSettings {
    /// <summary>Base URL, e.g. <c>http://localhost:8080/</c>.</summary>
    public string BaseUrl { get; set; } = "budgetfriend.zaidalkallas.me";

    /// <summary>API version segment appended to the base path.</summary>
    public string Version { get; set; } = "1";

    /// <summary>
    /// Google OAuth client ID used for "Sign in with Google". Empty/unset
    /// hides the Google button on the login page.
    /// </summary>
    public string GoogleClientId { get; set; } = "";

    /// <summary>Computed path prefix, e.g. <c>api/v1</c>.</summary>
    public string BasePath => $"api/v{Version}";
}