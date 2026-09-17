using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Data.Remote.ApiServices;
using BudgetFriend.App.Data.Remote.FeatureServices;
using BudgetFriend.Web.Components;
using BudgetFriend.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// Real auth is client-held JWT (AuthService), so [Authorize]-protected routes
// must be able to pass the HTTP-level authorization middleware. The pass-through
// scheme always authenticates; MainLayout enforces who may see what.
builder.Services.AddAuthentication(PassThroughAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, PassThroughAuthenticationHandler>(
        PassThroughAuthenticationHandler.SchemeName, null);

// Api configuration from appsettings "BudgetFriend" section.
var apiSettings = builder.Configuration.GetSection("BudgetFriend").Get<ApiSettings>() ?? new ApiSettings();
builder.Services.AddSingleton(apiSettings);

// HTTP + authentication plumbing.
builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiHttpClient>();
builder.Services.AddScoped<ITokenStore, WebTokenStore>();
builder.Services.AddScoped<IPreferenceStore, WebPreferenceStore>();
builder.Services.AddSingleton<ISystemThemeProvider, WebSystemThemeProvider>();
builder.Services.AddSingleton<IConnectivityService, WebConnectivityService>();

// App-level state services (scoped == per circuit).
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<FinanceFormatter>();

// Typed remote API services.
builder.Services.AddScoped<IAuthApi, AuthApi>();
builder.Services.AddScoped<IAccountsApi, AccountsApi>();
builder.Services.AddScoped<ICategoriesApi, CategoriesApi>();
builder.Services.AddScoped<ITransactionsApi, TransactionsApi>();
builder.Services.AddScoped<ITransfersApi, TransfersApi>();
builder.Services.AddScoped<IDashboardApi, DashboardApi>();

// Auth: dual AuthenticationStateProvider + IAuthService.
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthService>());
builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<AuthService>());

// Online-first feature services.
builder.Services.AddScoped<IAccountService, RemoteAccountService>();
builder.Services.AddScoped<ICategoryService, RemoteCategoryService>();
builder.Services.AddScoped<ITransactionService, RemoteTransactionService>();
builder.Services.AddScoped<ITransferService, RemoteTransferService>();
builder.Services.AddScoped<IDashboardService, RemoteDashboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(BudgetFriend.App.AppRoutes).Assembly);

await app.RunAsync();
