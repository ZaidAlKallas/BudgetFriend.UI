using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Data.Remote.ApiServices;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.FeatureServices;

/// <summary>Online-first account service backed by the remote API.</summary>
public sealed class RemoteAccountService : IAccountService {
    private readonly IAccountsApi _api;

    public RemoteAccountService(IAccountsApi api) {
        _api = api;
    }

    public Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default)
        => _api.GetAccountsAsync(ct);

    public Task<AccountDto> GetAccountAsync(Guid id, CancellationToken ct = default)
        => _api.GetAccountAsync(id, ct);

    public Task<AccountMutationResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default)
        => _api.CreateAccountAsync(request, ct);

    public Task<AccountMutationResult> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken ct = default)
        => _api.UpdateAccountAsync(id, request, ct);

    public Task DeleteAccountAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAccountAsync(id, ct);

    public Task<AccountDetailResponse> GetAccountDetailAsync(Guid id, TransactionQuery query, CancellationToken ct = default)
        => _api.GetAccountWithTransactionsAsync(id, query, ct);
}

/// <summary>Online-first category service backed by the remote API.</summary>
public sealed class RemoteCategoryService : ICategoryService {
    private readonly ICategoriesApi _api;

    public RemoteCategoryService(ICategoriesApi api) {
        _api = api;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => _api.GetCategoriesAsync(ct);

    public Task<CategoryDto> GetCategoryAsync(Guid id, CancellationToken ct = default)
        => _api.GetCategoryAsync(id, ct);

    public Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
        => _api.CreateCategoryAsync(request, ct);

    public Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
        => _api.UpdateCategoryAsync(id, request, ct);

    public Task DeleteCategoryAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteCategoryAsync(id, ct);
}

/// <summary>Online-first transaction service backed by the remote API.</summary>
public sealed class RemoteTransactionService : ITransactionService {
    private readonly ITransactionsApi _api;

    public RemoteTransactionService(ITransactionsApi api) {
        _api = api;
    }

    public Task<PagedResult<TransactionDto>> GetTransactionsAsync(TransactionQuery query, CancellationToken ct = default)
        => _api.GetTransactionsAsync(query, ct);

    public Task<TransactionDto> GetTransactionAsync(Guid id, CancellationToken ct = default)
        => _api.GetTransactionAsync(id, ct);

    public Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default)
        => _api.CreateTransactionAsync(request, ct);

    public Task<TransactionDto> UpdateTransactionAsync(Guid id, UpdateTransactionRequest request, CancellationToken ct = default)
        => _api.UpdateTransactionAsync(id, request, ct);

    public Task DeleteTransactionAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteTransactionAsync(id, ct);
}

/// <summary>Online-first transfer service backed by the remote API.</summary>
public sealed class RemoteTransferService : ITransferService {
    private readonly ITransfersApi _api;

    public RemoteTransferService(ITransfersApi api) {
        _api = api;
    }

    public Task<IReadOnlyList<TransferDto>> GetTransfersAsync(CancellationToken ct = default)
        => _api.GetTransfersAsync(ct);

    public Task<TransferDto> GetTransferAsync(Guid id, CancellationToken ct = default)
        => _api.GetTransferAsync(id, ct);

    public Task<TransferDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default)
        => _api.CreateTransferAsync(request, ct);

    public Task DeleteTransferAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteTransferAsync(id, ct);
}