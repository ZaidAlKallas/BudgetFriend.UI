using BudgetFriend.App.Models;

namespace BudgetFriend.App.Application.Services;

/// <summary>A date period used for dashboard analysis.</summary>
public sealed record DateRange(DateTimeOffset From, DateTimeOffset To);

/// <summary>
/// Accounts. Web implementation hits the remote API; Mobile implementation
/// reads/writes local SQLite. The UI depends only on this contract.
/// </summary>
public interface IAccountService {
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default);
    Task<AccountDto> GetAccountAsync(Guid id, CancellationToken ct = default);
    Task<AccountMutationResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<AccountMutationResult> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken ct = default);
    Task DeleteAccountAsync(Guid id, CancellationToken ct = default);
    Task<AccountDetailResponse> GetAccountDetailAsync(Guid id, TransactionQuery query, CancellationToken ct = default);
}

/// <summary>Categories.</summary>
public interface ICategoryService {
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto> GetCategoryAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Transactions.</summary>
public interface ITransactionService {
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(TransactionQuery query, CancellationToken ct = default);
    Task<TransactionDto> GetTransactionAsync(Guid id, CancellationToken ct = default);
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default);
    Task<TransactionDto> UpdateTransactionAsync(Guid id, UpdateTransactionRequest request, CancellationToken ct = default);
    Task DeleteTransactionAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Transfers.</summary>
public interface ITransferService {
    Task<IReadOnlyList<TransferDto>> GetTransfersAsync(CancellationToken ct = default);
    Task<TransferDto> GetTransferAsync(Guid id, CancellationToken ct = default);
    Task<TransferDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default);
    Task DeleteTransferAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Dashboard/financial summary.</summary>
public interface IDashboardService {
    Task<DashboardOverview> GetOverviewAsync(CancellationToken ct = default);
    Task<DashboardSummaryResponse> GetSummaryAsync(DateRange? period = null, CancellationToken ct = default);
    Task<CategoryAnalysisResponse> GetCategoryAnalysisAsync(DateRange? period = null, CancellationToken ct = default);
}