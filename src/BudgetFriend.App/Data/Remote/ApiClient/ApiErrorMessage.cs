using System.Net;

namespace BudgetFriend.App.Data.Remote.ApiClient;

/// <summary>
/// Captures a friendly, structured description of a failed operation so the
/// UI can render it elegantly regardless of where the failure originated
/// (HTTP status, network, serialization, timeout, ...).
/// </summary>
public sealed class ApiErrorMessage {
    public int? StatusCode { get; }
    public string? Title { get; }
    public string? Detail { get; }
    public IReadOnlyList<string> FieldErrors { get; }

    public bool IsClientError => StatusCode is >= 400 and < 500;
    public bool IsServerError => StatusCode is >= 500;
    public bool IsUnauthorized => StatusCode == 401;
    public bool IsNotFound => StatusCode == 404;
    public bool IsValidationError => StatusCode == 400;

    private ApiErrorMessage(int? statusCode, string? title, string? detail, IReadOnlyList<string> fieldErrors) {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        FieldErrors = fieldErrors;
    }

    public static ApiErrorMessage From(Exception ex) {
        if (ex is ApiException api) {
            return FromApi(api);
        }

        if (ex is HttpRequestException) {
            return new ApiErrorMessage(null, "Error.Network", null, []);
        }

        if (ex is TaskCanceledException or OperationCanceledException) {
            return new ApiErrorMessage(null, "Error.Timeout", null, []);
        }

        return new ApiErrorMessage(null, "Error.Unexpected", ex.Message, []);
    }

    private static ApiErrorMessage FromApi(ApiException api) {
        var problem = api.Problem;
        var fieldErrors = problem is { HasFieldErrors: true }
            ? problem.Errors.Values.SelectMany(v => v).ToList()
            : new List<string>();

        var detail = problem is { Detail: not null } and { Detail.Length: > 0 }
            ? problem.Detail
            : api.Message;

        return new ApiErrorMessage(api.StatusCode, TitleKeyFor(api.StatusCode), detail, fieldErrors);
    }

    /// <summary>
    /// Returns a localization key describing the status code (e.g.
    /// "Error.Http401") or a generic key when no code is present.
    /// </summary>
    private static string TitleKeyFor(int statusCode) => statusCode switch {
        400 => "Error.Http400",
        401 => "Error.Http401",
        403 => "Error.Http403",
        404 => "Error.Http404",
        405 => "Error.Http405",
        409 => "Error.Http409",
        422 => "Error.Http422",
        429 => "Error.Http429",
        >= 500 and < 600 => "Error.Http5xx",
        _ => "Error.Unexpected"
    };
}