using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.ApiServices;

/// <summary>Transaction endpoints of the BudgetFriend.API.</summary>
public interface ITransactionsApi {
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(TransactionQuery query, CancellationToken ct = default);
    Task<TransactionDto> GetTransactionAsync(Guid transactionId, CancellationToken ct = default);
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default);
    Task<TransactionDto> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequest request, CancellationToken ct = default);
    Task DeleteTransactionAsync(Guid transactionId, CancellationToken ct = default);
    Task<PagedResult<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, TransactionQuery query, CancellationToken ct = default);
}

public sealed class TransactionsApi : ITransactionsApi {
    private readonly ApiHttpClient _client;

    public TransactionsApi(ApiHttpClient client) {
        _client = client;
    }

    public Task<PagedResult<TransactionDto>> GetTransactionsAsync(TransactionQuery query, CancellationToken ct = default)
        => _client.GetAsync<PagedResult<TransactionDto>>($"{_client.Path("transactions")}{BuildQuery(query)}", ct);

    public Task<PagedResult<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, TransactionQuery query, CancellationToken ct = default)
        => _client.GetAsync<PagedResult<TransactionDto>>($"{_client.Path("transactions")}{BuildQuery(query with { AccountId = accountId })}", ct);

    public Task<TransactionDto> GetTransactionAsync(Guid transactionId, CancellationToken ct = default)
        => _client.GetAsync<TransactionDto>($"{_client.Path("transactions")}/{transactionId}", ct);

    public Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default)
        => _client.PostAsync<CreateTransactionRequest, TransactionDto>(_client.Path("transactions"), request, ct: ct);

    public Task<TransactionDto> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequest request, CancellationToken ct = default)
        => _client.PutAsync<UpdateTransactionRequest, TransactionDto>($"{_client.Path("transactions")}/{transactionId}", request, ct);

    public Task DeleteTransactionAsync(Guid transactionId, CancellationToken ct = default)
        => _client.DeleteAsync($"{_client.Path("transactions")}/{transactionId}", ct);

    internal static string BuildQuery(TransactionQuery query) {
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
        return QueryStringBuilder.Build(parameters.ToArray());
    }
}