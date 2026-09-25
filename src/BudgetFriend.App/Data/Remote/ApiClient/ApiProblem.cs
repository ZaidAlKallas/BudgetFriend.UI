using System.Text.Json;

namespace BudgetFriend.App.Data.Remote.ApiClient;

/// <summary>
/// The API reports failures using RFC 7807 <c>application/problem+json</c>.
/// The <see cref="Status"/> tolerates the API occasionally emitting it as a
/// string instead of an integer.
/// </summary>
public sealed class ApiProblem {
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public Dictionary<string, string[]> Errors { get; set; } = [];

    public bool HasFieldErrors => Errors.Count > 0;

    public static ApiProblem? TryParse(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }

        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String) {
                return new ApiProblem { Detail = root.GetString() };
            }

            if (root.ValueKind != JsonValueKind.Object) {
                return null;
            }

            var problem = new ApiProblem {
                Type = GetString(root, "type"),
                Title = GetString(root, "title"),
                Detail = GetString(root, "detail") ?? GetString(root, "message"),
                Instance = GetString(root, "instance")
            };

            if (root.TryGetProperty("status", out var status)) {
                problem.Status = status.ValueKind switch {
                    JsonValueKind.Number when status.TryGetInt32(out var statusCode) => statusCode,
                    JsonValueKind.String when int.TryParse(status.GetString(), out var s) => s,
                    _ => 0
                };
            }

            if (root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object) {
                foreach (var prop in errors.EnumerateObject()) {
                    if (prop.Value.ValueKind == JsonValueKind.Array) {
                        problem.Errors[prop.Name] = prop.Value
                            .EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .ToArray();
                    }
                }
            }

            return problem;
        }
        catch (JsonException) {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string property) {
        return root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}