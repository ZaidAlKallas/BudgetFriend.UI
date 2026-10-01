using System.Reflection;
using BudgetFriend.App.Components.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace BudgetFriend.App.Tests;

/// <summary>
/// The legal documents must stay reachable without signing in, so they need a
/// stable public path and must not pick up <c>[Authorize]</c>. Route guarding
/// happens in MainLayout rather than the endpoint, so nothing else would fail
/// the build if that protection was added here by accident.
/// </summary>
public class LegalPageRouteTests {
    [Theory]
    [InlineData(typeof(Privacy), "/privacy")]
    [InlineData(typeof(Terms), "/terms")]
    public void Legal_page_is_routed_at_its_public_path(Type component, string expectedPath) {
        var route = component.GetCustomAttributes<RouteAttribute>().Single();

        Assert.Equal(expectedPath, route.Template);
        Assert.Null(component.GetCustomAttribute<AuthorizeAttribute>());
    }
}