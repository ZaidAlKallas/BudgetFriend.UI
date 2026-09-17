using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Data.Remote.ApiServices;
using BudgetFriend.Mobile.Data;
using BudgetFriend.Mobile.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace BudgetFriend.Mobile;

public static class MauiProgram {
    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthorizationCore();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        RegisterServices(builder.Services);

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services) {
        services.AddHttpClient();

        services.AddSingleton<ApiSettings>(_ => new ApiSettings {
            BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080/"
                : "http://localhost:8080/"
        });

        // Platform services.
        services.AddScoped<ITokenStore, SecureTokenStore>();
        services.AddScoped<IPreferenceStore, MauiPreferenceStore>();
        services.AddSingleton<ISystemThemeProvider, MauiSystemThemeProvider>();
        services.AddSingleton<IConnectivityService, MauiConnectivityService>();

        // App-level state services (scoped == the single BlazorWebView scope).
        services.AddScoped<LocalizationService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<FinanceFormatter>();

        // Auth: the dual AuthenticationStateProvider + IAuthService.
        services.AddScoped<IAuthApi, AuthApi>();
        services.AddScoped<ApiHttpClient>();
        services.AddScoped<AuthService>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthService>());
        services.AddScoped<IAuthService>(sp => sp.GetRequiredService<AuthService>());

        // Local (offline-first) feature services backed by SQLite.
        services.AddSingleton<LocalDatabase>();
        services.AddScoped<LocalFinanceServices>();
        services.AddScoped<IAccountService>(sp => sp.GetRequiredService<LocalFinanceServices>());
        services.AddScoped<ICategoryService>(sp => sp.GetRequiredService<LocalFinanceServices>());
        services.AddScoped<ITransactionService>(sp => sp.GetRequiredService<LocalFinanceServices>());
        services.AddScoped<ITransferService>(sp => sp.GetRequiredService<LocalFinanceServices>());
        services.AddScoped<IDashboardService>(sp => sp.GetRequiredService<LocalFinanceServices>());
    }
}