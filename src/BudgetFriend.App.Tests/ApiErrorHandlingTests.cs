using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Data.Remote.ApiServices;
using BudgetFriend.App.Models;
using System.Net;

namespace BudgetFriend.App.Tests;

public class ApiErrorHandlingTests {
    private const string DuplicateCategoryMessage = "A category with the name 'Food' and type 'Expense' already exists.";

    [Fact]
    public void TryParse_json_string_uses_message_as_detail() {
        var problem = ApiProblem.TryParse($"\"{DuplicateCategoryMessage}\"");

        Assert.NotNull(problem);
        Assert.Equal(DuplicateCategoryMessage, problem.Detail);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("409")]
    [InlineData("[]")]
    public void TryParse_non_object_returns_null(string json) {
        Assert.Null(ApiProblem.TryParse(json));
    }

    [Fact]
    public void TryParse_problem_object_reads_fields() {
        var problem = ApiProblem.TryParse("""
            {
              "type": "https://example.com/probs/conflict",
              "title": "Conflict",
              "status": 409,
              "detail": "The category already exists.",
              "errors": {
                "name": ["Name is already in use."]
              }
            }
            """);

        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("The category already exists.", problem.Detail);
        Assert.Equal(["Name is already in use."], problem.Errors["name"]);
    }

    [Fact]
    public async Task Category_api_json_string_error_preserves_server_message() {
        using var http = new HttpClient(new StubHandler(
            HttpStatusCode.Conflict,
            $"\"{DuplicateCategoryMessage}\""));
        var client = new ApiHttpClient(http, new ApiSettings { BaseUrl = "https://example.com" }, new StubTokenStore());
        var api = new CategoriesApi(client);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            api.CreateCategoryAsync(new CreateCategoryRequest("Food", TransactionType.Expense)));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(DuplicateCategoryMessage, exception.Message);

        var error = ApiErrorMessage.From(exception);
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(DuplicateCategoryMessage, error.Detail);
    }

    [Fact]
    public async Task Bad_request_message_object_preserves_server_message() {
        const string message = "Insufficient balance in the source account.";
        using var http = new HttpClient(new StubHandler(
            HttpStatusCode.BadRequest,
            $"{{\"message\": \"{message}\"}}"));
        var client = new ApiHttpClient(http, new ApiSettings { BaseUrl = "https://example.com" }, new StubTokenStore());

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<object>("transfers"));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal(message, exception.Message);

        var error = ApiErrorMessage.From(exception);
        Assert.Equal(message, error.Detail);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            return Task.FromResult(new HttpResponseMessage(statusCode) {
                Content = new StringContent(content)
            });
        }
    }

    private sealed class StubTokenStore : ITokenStore {
        public Task<TokenSession?> GetAsync() => Task.FromResult<TokenSession?>(null);
        public Task SetAsync(TokenSession session) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }
}
