namespace BudgetFriend.App.Models;

/// <summary>Single account entry inside the dashboard overview.</summary>
public sealed record DashboardAccountDto(Guid Id, string Name, decimal Balance, Currency Currency);

/// <summary>Currency-amount pair used inside dashboard breakdowns.</summary>
public sealed record CurrencyAmountDto(Currency Currency, decimal TotalAmount, int Count);

/// <summary>Frequent expense category with amounts by currency.</summary>
public sealed record FrequentCategoryDto(
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<CurrencyAmountDto> AmountsByCurrency);

/// <summary>Total balance / monthly figures grouped by currency.</summary>
public sealed record CurrencyBreakdownDto(
    Currency Currency,
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal NetMonthlyIncome);

/// <summary>Response of GET /dashboard.</summary>
public sealed record DashboardOverview(
    IReadOnlyList<DashboardAccountDto> Accounts,
    IReadOnlyList<FrequentCategoryDto> FrequentExpenseCategories,
    IReadOnlyList<DashboardTransactionDto> RecentTransactions,
    IReadOnlyList<CurrencyBreakdownDto> CurrencyBreakdown);

/// <summary>Response of GET /dashboard/summary.</summary>
public sealed record DashboardSummaryResponse(IReadOnlyList<PeriodSummaryDto> Summaries);

/// <summary>Financial summary for one currency within a period.</summary>
public sealed record PeriodSummaryDto(
    Currency Currency,
    decimal InitialBalance,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal TransferIn,
    decimal TransferOut,
    decimal NetAmount);

/// <summary>Response of GET /dashboard/categories-analysis.</summary>
public sealed record CategoryAnalysisResponse(IReadOnlyList<CategoryBreakdownDto> CategoryBreakdown);

/// <summary>Category totals for a period.</summary>
public sealed record CategoryBreakdownDto(
    Guid CategoryId,
    string CategoryName,
    TransactionType? TransactionType,
    IReadOnlyList<CurrencyAmountDto> CurrencyBreakdown);