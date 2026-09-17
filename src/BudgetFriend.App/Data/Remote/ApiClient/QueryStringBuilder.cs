using System.Globalization;

namespace BudgetFriend.App.Data.Remote.ApiClient;

/// <summary>Small helper to build URL query strings.</summary>
public static class QueryStringBuilder {
    public static string Build(QueryParameter[] parameters) {
        var parts = new List<string>();
        foreach (var p in parameters) {
            parts.Add($"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.Value)}");
        }
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    public sealed record QueryParameter(string Name, string Value);

    public static QueryParameter? Date(string name, DateTimeOffset? value)
        => value is null
            ? null
            : new QueryParameter(name, value.Value.ToUniversalTime().UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
}