using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.ApiServices;

/// <summary>Account endpoints of the BudgetFriend.API.</summary>
public interface IAccountsApi {
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default);
    Task<AccountMutationResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<AccountDto> GetAccountAsync(Guid accountId, CancellationToken ct = default);
    Task<AccountMutationResult> UpdateAccountAsync(Guid accountId, UpdateAccountRequest request, CancellationToken ct = default);
    Task DeleteAccountAsync(Guid accountId, CancellationToken ct = default);
    Task<AccountDetailResponse> GetAccountWithTransactionsAsync(Guid accountId, TransactionQuery query, CancellationToken ct = default);
}

public sealed class AccountsApi(ApiHttpClient client) : IAccountsApi {
    private readonly ApiHttpClient _client = client;

    public Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default)
        => _client.GetAsync<IReadOnlyList<AccountDto>>(_client.Path("accounts"), ct);

    public Task<AccountMutationResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default)
        => _client.PostAsync<CreateAccountRequest, AccountMutationResult>(_client.Path("accounts"), request, ct: ct);

    public Task<AccountDto> GetAccountAsync(Guid accountId, CancellationToken ct = default)
        => _client.GetAsync<AccountDto>($"{_client.Path("accounts")}/{accountId}", ct);

    public Task<AccountMutationResult> UpdateAccountAsync(Guid accountId, UpdateAccountRequest request, CancellationToken ct = default)
        => _client.PutAsync<UpdateAccountRequest, AccountMutationResult>($"{_client.Path("accounts")}/{accountId}", request, ct);

    public Task DeleteAccountAsync(Guid accountId, CancellationToken ct = default)
        => _client.DeleteAsync($"{_client.Path("accounts")}/{accountId}", ct);

    public Task<AccountDetailResponse> GetAccountWithTransactionsAsync(Guid accountId, TransactionQuery query, CancellationToken ct = default) {
        var qs = BuildTransactionQuery(query);
        return _client.GetAsync<AccountDetailResponse>($"{_client.Path("accounts")}/{accountId}/transactions{qs}", ct);
    }

    internal static string BuildTransactionQuery(TransactionQuery query) {
        var parameters = new List<QueryStringBuilder.QueryParameter>
        {
            new("PageNumber", query.PageNumber.ToString()),
            new("PageSize", query.PageSize.ToString()),
        };
        if (query.AccountId is not null)
            parameters.Add(new("AccountId", query.AccountId.Value.ToString()));
        if (query.CategoryId is not null)
            parameters.Add(new("CategoryId", query.CategoryId.Value.ToString()));
        if (query.TransactionType is not null)
            parameters.Add(new("TransactionType", query.TransactionType.Value.ToString()));
        var from = QueryStringBuilder.Date("DateFrom", query.DateFrom);
        if (from is not null)
            parameters.Add(from);
        var to = QueryStringBuilder.Date("DateTo", query.DateTo);
        if (to is not null)
            parameters.Add(to);
        if (!string.IsNullOrWhiteSpace(query.Search))
            parameters.Add(new("Search", query.Search));
        return QueryStringBuilder.Build([.. parameters]);
    }
}