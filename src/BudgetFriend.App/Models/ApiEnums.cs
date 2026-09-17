namespace BudgetFriend.App.Models;

/// <summary>
/// Represents the type of a transaction. Values match the BudgetFriend.API enum.
/// </summary>
public enum TransactionType {
    Income,
    Expense,
    TransferIn,
    TransferOut
}

public enum Currency {
    USD,
    EUR,
    GBP,
    TRY,
    SAR,
    SYP,
    AED,
    CHF
}