using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.ApiServices;

/// <summary>Dashboard endpoints of the BudgetFriend.API.</summary>
public interface IDashboardApi {
    Task<DashboardOverview> GetOverviewAsync(CancellationToken ct = default);
    Task<DashboardSummaryResponse> GetSummaryAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default);
    Task<CategoryAnalysisResponse> GetCategoryAnalysisAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default);
}

public sealed class DashboardApi : IDashboardApi {
    private readonly ApiHttpClient _client;

    public DashboardApi(ApiHttpClient client) {
        _client = client;
    }

    public Task<DashboardOverview> GetOverviewAsync(CancellationToken ct = default)
        => _client.GetAsync<DashboardOverview>(_client.Path("dashboard"), ct);

    public Task<DashboardSummaryResponse> GetSummaryAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default)
        => _client.GetAsync<DashboardSummaryResponse>($"{_client.Path("dashboard/summary")}{BuildPeriod(fromDate, toDate)}", ct);

    public Task<CategoryAnalysisResponse> GetCategoryAnalysisAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default)
        => _client.GetAsync<CategoryAnalysisResponse>($"{_client.Path("dashboard/categories-analysis")}{BuildPeriod(fromDate, toDate)}", ct);

    private static string BuildPeriod(DateTimeOffset? fromDate, DateTimeOffset? toDate) {
        var parameters = new List<QueryStringBuilder.QueryParameter>();
        var from = QueryStringBuilder.Date("fromDate", fromDate);
        if (from is not null)
            parameters.Add(from);
        var to = QueryStringBuilder.Date("toDate", toDate);
        if (to is not null)
            parameters.Add(to);
        return QueryStringBuilder.Build(parameters.ToArray());
    }
}