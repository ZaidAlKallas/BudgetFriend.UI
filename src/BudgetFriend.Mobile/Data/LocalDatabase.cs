using BudgetFriend.App.Models;
using SQLite;

namespace BudgetFriend.Mobile.Data;

[Table("accounts")]
public sealed class AccountEntity {
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public string Name { get; set; } = "";

    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public Currency Currency { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

[Table("categories")]
public sealed class CategoryEntity {
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public string Name { get; set; } = "";

    /// <summary>Stored as the integer value of <see cref="TransactionType"/>, or null for "any".</summary>
    public int? TransactionType { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

[Table("transactions")]
public sealed class TransactionEntity {
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid AccountId { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>Positive for income, negative for expense.</summary>
    public decimal Amount { get; set; }

    public Currency Currency { get; set; }
    public string? Note { get; set; }

    [Indexed]
    public DateTime TransactionDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public short TransactionType { get; set; } // 0 Income, 1 Expense, 2 TransferIn, 3 TransferOut
}

[Table("transfers")]
public sealed class TransferEntity {
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid FromAccountId { get; set; }

    [Indexed]
    public Guid ToAccountId { get; set; }

    public decimal FromAmount { get; set; }
    public decimal ToAmount { get; set; }
    public string? Note { get; set; }

    [Indexed]
    public DateTime TransferDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Central access to the local SQLite database. The connection is created
/// lazily and reused for the life of the app.
/// </summary>
public sealed class LocalDatabase : IDisposable {
    private readonly Lazy<SQLiteAsyncConnection> _connection;

    public LocalDatabase() {
        _connection = new Lazy<SQLiteAsyncConnection>(() => {
            var path = Path.Combine(FileSystem.AppDataDirectory, "budgetfriend.db3");
            var connection = new SQLiteAsyncConnection(
                path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            connection.CreateTableAsync<AccountEntity>().GetAwaiter().GetResult();
            connection.CreateTableAsync<CategoryEntity>().GetAwaiter().GetResult();
            connection.CreateTableAsync<TransactionEntity>().GetAwaiter().GetResult();
            connection.CreateTableAsync<TransferEntity>().GetAwaiter().GetResult();

            return connection;
        });
    }

    public SQLiteAsyncConnection Connection => _connection.Value;

    public void Dispose() => _connection.Value.CloseAsync().GetAwaiter().GetResult();
}