using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.ApiServices;

/// <summary>Transfer endpoints of the BudgetFriend.API.</summary>
public interface ITransfersApi {
    Task<IReadOnlyList<TransferDto>> GetTransfersAsync(CancellationToken ct = default);
    Task<TransferDto> GetTransferAsync(Guid transferId, CancellationToken ct = default);
    Task<TransferDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default);
    Task DeleteTransferAsync(Guid transferId, CancellationToken ct = default);
}

public sealed class TransfersApi : ITransfersApi {
    private readonly ApiHttpClient _client;

    public TransfersApi(ApiHttpClient client) {
        _client = client;
    }

    public Task<IReadOnlyList<TransferDto>> GetTransfersAsync(CancellationToken ct = default)
        => _client.GetAsync<IReadOnlyList<TransferDto>>(_client.Path("transfers"), ct);

    public Task<TransferDto> GetTransferAsync(Guid transferId, CancellationToken ct = default)
        => _client.GetAsync<TransferDto>($"{_client.Path("transfers")}/{transferId}", ct);

    public Task<TransferDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default)
        => _client.PostAsync<CreateTransferRequest, TransferDto>(_client.Path("transfers"), request, ct: ct);

    public Task DeleteTransferAsync(Guid transferId, CancellationToken ct = default)
        => _client.DeleteAsync($"{_client.Path("transfers")}/{transferId}", ct);
}