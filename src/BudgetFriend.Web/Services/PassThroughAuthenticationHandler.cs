using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BudgetFriend.Web.Services;

/// <summary>
/// Pass-through authentication used only by the Web host.
///
/// BudgetFriend authenticates with a client-held JWT via <c>AuthService</c>,
/// not with server cookies, so there is no server-side session to validate.
/// This scheme always produces an authenticated principal, which lets routes
/// decorated with <c>[Authorize]</c> pass the HTTP-level authorization
/// middleware and prerender. Real access control is enforced client-side in
/// <c>MainLayout</c> using <c>AuthService</c>.
/// </summary>
public sealed class PassThroughAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
    public const string SchemeName = "BudgetFriend.PassThrough";

    public PassThroughAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        // The antiforgery system requires authenticated identities to carry a
        // Name claim, so include a stable one (the same for every request).
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "budgetfriend-passthrough") },
            authenticationType: SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}