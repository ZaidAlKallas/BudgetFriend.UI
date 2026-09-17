using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Data.Remote.ApiServices;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.FeatureServices;

/// <summary>Online-first dashboard service backed by the remote API.</summary>
public sealed class RemoteDashboardService : IDashboardService {
    private readonly IDashboardApi _api;

    public RemoteDashboardService(IDashboardApi api) {
        _api = api;
    }

    public Task<DashboardOverview> GetOverviewAsync(CancellationToken ct = default)
        => _api.GetOverviewAsync(ct);

    public Task<DashboardSummaryResponse> GetSummaryAsync(DateRange? period = null, CancellationToken ct = default)
        => _api.GetSummaryAsync(period?.From, period?.To, ct);

    public Task<CategoryAnalysisResponse> GetCategoryAnalysisAsync(DateRange? period = null, CancellationToken ct = default)
        => _api.GetCategoryAnalysisAsync(period?.From, period?.To, ct);
}