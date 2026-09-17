namespace BudgetFriend.App.Models;

// Authentication
public sealed record RegisterRequest(string Email, string Password, string FirstName, string? LastName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record VerifyEmailRequest(string Token);
public sealed record ResendVerificationRequest(string Email);
public sealed record GoogleLoginRequest(string IdToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);

// Accounts
public sealed record CreateAccountRequest(string Name, decimal InitialBalance, Currency Currency);
public sealed record UpdateAccountRequest(string Name, decimal InitialBalance);

// Categories
public sealed record CreateCategoryRequest(string Name, TransactionType TransactionType);
public sealed record UpdateCategoryRequest(string Name, TransactionType TransactionType);

// Transactions
public sealed record CreateTransactionRequest(
    Guid AccountId,
    Guid? CategoryId,
    decimal Amount,
    TransactionType TransactionType,
    string? Note,
    DateTimeOffset? TransactionDate);

public sealed record UpdateTransactionRequest(decimal Amount, TransactionType TransactionType, string? Note, DateTimeOffset TransactionDate);

// Transfers
public sealed record CreateTransferRequest(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal FromAmount,
    decimal ToAmount,
    string? Note,
    DateTimeOffset? TransferDate);

/// <summary>Query parameters for paginated/filtered transaction lists.</summary>
public sealed record TransactionQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    TransactionType? TransactionType = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? Search = null);