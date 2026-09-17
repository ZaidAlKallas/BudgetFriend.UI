namespace BudgetFriend.App.Models;

/// <summary>Authentication tokens returned by the API.</summary>
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpireAtUtc);

/// <summary>Profile of the authenticated user.</summary>
public sealed record UserProfile(
    Guid Id,
    string Email,
    string FirstName,
    string? LastName,
    bool IsEmailVerified,
    DateTimeOffset CreatedAtUtc);

/// <summary>Account as returned by list/get endpoints.</summary>
public sealed record AccountDto(
    Guid Id,
    string Name,
    decimal InitialBalance,
    decimal CurrentBalance,
    Currency Currency);

/// <summary>Account returned when creating/updating an account.</summary>
public sealed record AccountMutationResult(
    Guid Id,
    string Name,
    decimal InitialBalance,
    Currency Currency);

/// <summary>Account together with its paginated transactions and balances.</summary>
public sealed record AccountDetailResponse(
    Guid Id,
    string Name,
    decimal InitialBalance,
    decimal CurrentBalance,
    decimal FilteredBalance,
    Currency Currency,
    PagedResult<TransactionDto> Transactions);

/// <summary>Category as returned by the API.</summary>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    TransactionType? TransactionType);

/// <summary>Transaction as returned by the API.</summary>
public sealed record TransactionDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Guid? CategoryId,
    string? CategoryName,
    TransactionType TransactionType,
    Currency Currency,
    decimal Amount,
    string? Note,
    DateTimeOffset TransactionDate,
    DateTimeOffset CreatedAtUtc);

/// <summary>Transaction shape used by the dashboard overview.</summary>
public sealed record DashboardTransactionDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Currency Currency,
    Guid? CategoryId,
    string? CategoryName,
    TransactionType? TransactionType,
    decimal Amount,
    string? Note,
    DateTimeOffset TransactionDate);

/// <summary>Transfer between two accounts as returned by the API.</summary>
public sealed record TransferDto(
    Guid Id,
    Guid FromAccountId,
    string FromAccountName,
    Currency FromAccountCurrency,
    Guid ToAccountId,
    string ToAccountName,
    Currency ToAccountCurrency,
    decimal FromAmount,
    decimal ToAmount,
    string? Note,
    DateTimeOffset TransferDate,
    DateTimeOffset CreatedAtUtc);

/// <summary>Generic paginated result envelope used by the API.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);