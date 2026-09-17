namespace BudgetFriend.App.Data.Remote.ApiClient;

/// <summary>
/// Thrown when the API returns a non-success status code. Exposes the HTTP
/// status and, when available, the parsed RFC 7807 problem details.
/// </summary>
public sealed class ApiException : Exception {
    public ApiException(int statusCode, string? message = null, ApiProblem? problem = null, Exception? inner = null)
        : base(message ?? BuildMessage(statusCode, problem), inner) {
        StatusCode = statusCode;
        Problem = problem;
    }

    public int StatusCode { get; }

    public ApiProblem? Problem { get; }

    public bool IsUnauthorized => StatusCode == 401;

    public bool IsNotFound => StatusCode == 404;

    public bool IsConflict => StatusCode == 409;

    public bool IsValidationError => StatusCode == 400;

    private static string BuildMessage(int statusCode, ApiProblem? problem) {
        if (problem is not null) {
            var title = string.IsNullOrWhiteSpace(problem.Detail) ? problem.Title : problem.Detail;
            if (!string.IsNullOrWhiteSpace(title)) {
                return title;
            }
        }

        return statusCode switch {
            400 => "The request was not valid.",
            401 => "Authentication is required.",
            403 => "You do not have permission to perform this action.",
            404 => "The requested resource was not found.",
            409 => "The operation conflicts with the current state.",
            429 => "Too many requests. Please try again later.",
            _ => "An unexpected error occurred."
        };
    }
}