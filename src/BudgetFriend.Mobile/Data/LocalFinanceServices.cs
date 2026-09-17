using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Models;
using SQLite;

namespace BudgetFriend.Mobile.Data;

/// <summary>
/// Local (offline-first) implementation of the finance feature contracts.
/// All data lives in the on-device SQLite database; there is currently no
/// synchronization with the BudgetFriend.API (the sync boundary is
/// architecturally reserved for a future API capability).
/// </summary>
public sealed class LocalFinanceServices :
    IAccountService,
    ICategoryService,
    ITransactionService,
    ITransferService,
    IDashboardService {
    private readonly LocalDatabase _database;

    public LocalFinanceServices(LocalDatabase database) {
        _database = database;
    }

    private SQLiteAsyncConnection Db => _database.Connection;

    // ---- Accounts ------------------------------------------------------------

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default) {
        var rows = await Db.Table<AccountEntity>().OrderBy(a => a.Name).ToListAsync().ConfigureAwait(false);
        return rows.Select(ToDto).ToList();
    }

    public async Task<AccountDto> GetAccountAsync(Guid id, CancellationToken ct = default) {
        var row = await Db.FindAsync<AccountEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Account {id} not found.");
        return ToDto(row);
    }

    public async Task<AccountMutationResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default) {
        var now = DateTime.UtcNow;
        var entity = new AccountEntity {
            Id = Guid.NewGuid(),
            Name = request.Name,
            InitialBalance = request.InitialBalance,
            CurrentBalance = request.InitialBalance,
            Currency = request.Currency,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await Db.InsertAsync(entity).ConfigureAwait(false);
        return ToMutationResult(entity);
    }

    public async Task<AccountMutationResult> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken ct = default) {
        var row = await RequireAccountAsync(id).ConfigureAwait(false);
        var balanceDelta = request.InitialBalance - row.InitialBalance;

        row.Name = request.Name;
        row.InitialBalance = request.InitialBalance;
        row.CurrentBalance += balanceDelta;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await Db.UpdateAsync(row).ConfigureAwait(false);
        return ToMutationResult(row);
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken ct = default) {
        _ = await RequireAccountAsync(id).ConfigureAwait(false);

        await Db.ExecuteAsync("DELETE FROM transactions WHERE AccountId = ?", id).ConfigureAwait(false);
        await Db.ExecuteAsync("DELETE FROM transfers WHERE FromAccountId = ? OR ToAccountId = ?", id, id).ConfigureAwait(false);
        await Db.DeleteAsync<AccountEntity>(id).ConfigureAwait(false);
    }

    public async Task<AccountDetailResponse> GetAccountDetailAsync(Guid id, TransactionQuery query, CancellationToken ct = default) {
        var account = await RequireAccountAsync(id).ConfigureAwait(false);
        var matched = await QueryTransactions(query with { AccountId = id }).ConfigureAwait(false);
        var names = await BuildNameLookupAsync().ConfigureAwait(false);
        var page = Paginate(matched, query.PageNumber, query.PageSize);

        return new AccountDetailResponse(
            account.Id,
            account.Name,
            account.InitialBalance,
            account.CurrentBalance,
            matched.Sum(t => SignedDelta((TransactionType)t.TransactionType, t.Amount)),
            account.Currency,
            new PagedResult<TransactionDto>(
                page.Select(t => ToDto(t, names)).ToList(),
                query.PageNumber,
                query.PageSize,
                matched.Count,
                TotalPages(matched.Count, query.PageSize),
                query.PageNumber > 1,
                query.PageNumber < TotalPages(matched.Count, query.PageSize)));
    }

    // ---- Categories ----------------------------------------------------------

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) {
        var rows = await Db.Table<CategoryEntity>().OrderBy(c => c.Name).ToListAsync().ConfigureAwait(false);
        return rows.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> GetCategoryAsync(Guid id, CancellationToken ct = default) {
        var row = await Db.FindAsync<CategoryEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        return ToDto(row);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default) {
        var entity = new CategoryEntity {
            Id = Guid.NewGuid(),
            Name = request.Name,
            TransactionType = (int?)request.TransactionType,
            CreatedAtUtc = DateTime.UtcNow
        };

        await Db.InsertAsync(entity).ConfigureAwait(false);
        return ToDto(entity);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default) {
        var row = await Db.FindAsync<CategoryEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        row.Name = request.Name;
        row.TransactionType = (int?)request.TransactionType;
        await Db.UpdateAsync(row).ConfigureAwait(false);
        return ToDto(row);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct = default) {
        await Db.DeleteAsync<CategoryEntity>(id).ConfigureAwait(false);
    }

    // ---- Transactions --------------------------------------------------------

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(TransactionQuery query, CancellationToken ct = default) {
        var matched = await QueryTransactions(query).ConfigureAwait(false);
        var names = await BuildNameLookupAsync().ConfigureAwait(false);
        var page = Paginate(matched, query.PageNumber, query.PageSize);

        return new PagedResult<TransactionDto>(
            [.. page.Select(t => ToDto(t, names))],
            query.PageNumber,
            query.PageSize,
            matched.Count,
            TotalPages(matched.Count, query.PageSize),
            query.PageNumber > 1,
            query.PageNumber < TotalPages(matched.Count, query.PageSize));
    }

    public async Task<TransactionDto> GetTransactionAsync(Guid id, CancellationToken ct = default) {
        var row = await Db.FindAsync<TransactionEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");
        var names = await BuildNameLookupAsync().ConfigureAwait(false);
        return ToDto(row, names);
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default) {
        var account = await RequireAccountAsync(request.AccountId).ConfigureAwait(false);

        var entity = new TransactionEntity {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Amount = Math.Abs(request.Amount),
            Currency = account.Currency,
            Note = request.Note,
            TransactionDateUtc = request.TransactionDate?.UtcDateTime ?? DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            TransactionType = (short)request.TransactionType
        };

        await Db.InsertAsync(entity).ConfigureAwait(false);
        await AdjustAccountBalanceAsync(account.Id, SignedDelta(request.TransactionType, entity.Amount)).ConfigureAwait(false);

        var names = await BuildNameLookupAsync().ConfigureAwait(false);
        return ToDto(entity, names);
    }

    public async Task<TransactionDto> UpdateTransactionAsync(Guid id, UpdateTransactionRequest request, CancellationToken ct = default) {
        var row = await Db.FindAsync<TransactionEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");

        var oldDelta = SignedDelta((TransactionType)row.TransactionType, row.Amount);
        var newDelta = SignedDelta(request.TransactionType, Math.Abs(request.Amount));
        var balanceDelta = newDelta - oldDelta;

        row.Amount = Math.Abs(request.Amount);
        row.Note = request.Note;
        row.TransactionDateUtc = request.TransactionDate.UtcDateTime;
        row.TransactionType = (short)request.TransactionType;

        await Db.UpdateAsync(row).ConfigureAwait(false);
        await AdjustAccountBalanceAsync(row.AccountId, balanceDelta).ConfigureAwait(false);

        var names = await BuildNameLookupAsync().ConfigureAwait(false);
        return ToDto(row, names);
    }

    public async Task DeleteTransactionAsync(Guid id, CancellationToken ct = default) {
        var row = await Db.FindAsync<TransactionEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");

        await AdjustAccountBalanceAsync(row.AccountId, -SignedDelta((TransactionType)row.TransactionType, row.Amount)).ConfigureAwait(false);
        await Db.DeleteAsync(row).ConfigureAwait(false);
    }

    // ---- Transfers -----------------------------------------------------------

    public async Task<IReadOnlyList<TransferDto>> GetTransfersAsync(CancellationToken ct = default) {
        var rows = await Db.Table<TransferEntity>().OrderByDescending(t => t.TransferDateUtc).ToListAsync().ConfigureAwait(false);
        var names = await BuildTransferLookupAsync().ConfigureAwait(false);
        return [.. rows.Select(t => ToDto(t, names))];
    }

    public async Task<TransferDto> GetTransferAsync(Guid id, CancellationToken ct = default) {
        var row = await Db.FindAsync<TransferEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Transfer {id} not found.");
        var names = await BuildTransferLookupAsync().ConfigureAwait(false);
        return ToDto(row, names);
    }

    public async Task<TransferDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default) {
        _ = await RequireAccountAsync(request.FromAccountId).ConfigureAwait(false);
        _ = await RequireAccountAsync(request.ToAccountId).ConfigureAwait(false);

        if (request.FromAccountId == request.ToAccountId) {
            throw new ApiException(400, "From and To accounts must differ.");
        }

        var entity = new TransferEntity {
            Id = Guid.NewGuid(),
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            FromAmount = request.FromAmount,
            ToAmount = request.ToAmount,
            Note = request.Note,
            TransferDateUtc = request.TransferDate?.UtcDateTime ?? DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        await Db.InsertAsync(entity).ConfigureAwait(false);
        await Db.ExecuteAsync("UPDATE accounts SET CurrentBalance = CurrentBalance - ?, UpdatedAtUtc = ? WHERE Id = ?",
            request.FromAmount, DateTime.UtcNow, request.FromAccountId).ConfigureAwait(false);
        await Db.ExecuteAsync("UPDATE accounts SET CurrentBalance = CurrentBalance + ?, UpdatedAtUtc = ? WHERE Id = ?",
            request.ToAmount, DateTime.UtcNow, request.ToAccountId).ConfigureAwait(false);

        var names = await BuildTransferLookupAsync().ConfigureAwait(false);
        return ToDto(entity, names);
    }

    public async Task DeleteTransferAsync(Guid id, CancellationToken ct = default) {
        var row = await Db.FindAsync<TransferEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Transfer {id} not found.");

        await Db.ExecuteAsync("UPDATE accounts SET CurrentBalance = CurrentBalance + ?, UpdatedAtUtc = ? WHERE Id = ?",
            row.FromAmount, DateTime.UtcNow, row.FromAccountId).ConfigureAwait(false);
        await Db.ExecuteAsync("UPDATE accounts SET CurrentBalance = CurrentBalance - ?, UpdatedAtUtc = ? WHERE Id = ?",
            row.ToAmount, DateTime.UtcNow, row.ToAccountId).ConfigureAwait(false);

        await Db.DeleteAsync(row).ConfigureAwait(false);
    }

    // ---- Dashboard -----------------------------------------------------------

    public async Task<DashboardOverview> GetOverviewAsync(CancellationToken ct = default) {
        var accountRows = await Db.Table<AccountEntity>().ToListAsync().ConfigureAwait(false);
        var transactionRows = await Db.Table<TransactionEntity>().ToListAsync().ConfigureAwait(false);
        var categoryRows = await Db.Table<CategoryEntity>().ToListAsync().ConfigureAwait(false);

        var nameLookup = accountRows.ToDictionary(a => a.Id, a => a.Name);
        var categoryLookup = categoryRows.ToDictionary(c => c.Id, c => c.Name);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthTransactions = transactionRows
            .Where(t => t.TransactionDateUtc >= monthStart)
            .ToList();

        var recent = transactionRows
            .OrderByDescending(t => t.TransactionDateUtc)
            .Take(5);

        var frequent = ExpenseTransactionRows(transactionRows, monthStart)
            .GroupBy(t => t.CategoryId)
            .Select(g => new FrequentCategoryDto(
                g.Key ?? Guid.Empty,
                g.Key is { } k && categoryLookup.TryGetValue(k, out var n) ? n : "—",
                g.GroupBy(t => t.Currency)
                    .Select(c => new CurrencyAmountDto(c.Key, c.Sum(t => -t.Amount), c.Count()))
                    .ToList()))
            .OrderByDescending(g => g.AmountsByCurrency.Sum(a => a.TotalAmount))
            .Take(3)
            .ToList();

        var breakdown = accountRows
            .GroupBy(a => a.Currency)
            .Select(g => new CurrencyBreakdownDto(
                g.Key,
                g.Sum(a => a.CurrentBalance),
                monthTransactions.Where(t => IsIncomeType((TransactionType)t.TransactionType)).Sum(t => t.Amount),
                monthTransactions.Where(t => IsExpenseType((TransactionType)t.TransactionType)).Sum(t => t.Amount),
                monthTransactions.Sum(t => SignedDelta((TransactionType)t.TransactionType, t.Amount))))
            .ToList();

        return new DashboardOverview(
            accountRows.Select(a => new DashboardAccountDto(a.Id, a.Name, a.CurrentBalance, a.Currency)).ToList(),
            frequent,
            recent.Select(t => ToDashboardDto(t, categoryLookup, nameLookup)).ToList(),
            breakdown);
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(DateRange? period = null, CancellationToken ct = default) {
        var accounts = await Db.Table<AccountEntity>().ToListAsync().ConfigureAwait(false);
        var transactions = await Db.Table<TransactionEntity>().ToListAsync().ConfigureAwait(false);
        var transfers = await Db.Table<TransferEntity>().ToListAsync().ConfigureAwait(false);

        var (from, to) = period is { } p
            ? (p.From.UtcDateTime, p.To.UtcDateTime)
            : (DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime());

        var inPeriod = transactions.Where(t => t.TransactionDateUtc >= from && t.TransactionDateUtc <= to).ToList();
        var transfersInPeriod = transfers.Where(t => t.TransferDateUtc >= from && t.TransferDateUtc <= to).ToList();

        var accountCurrency = accounts.ToDictionary(a => a.Id, a => a.Currency);

        var summaries = accounts
            .GroupBy(a => a.Currency)
            .Select(g => {
                var income = inPeriod.Where(t => IsIncomeType((TransactionType)t.TransactionType)).Sum(t => t.Amount);
                var expenses = inPeriod.Where(t => IsExpenseType((TransactionType)t.TransactionType)).Sum(t => t.Amount);
                var transferIn = transfersInPeriod
                    .Where(t => accountCurrency.TryGetValue(t.ToAccountId, out var c) && c == g.Key)
                    .Sum(t => t.ToAmount);
                var transferOut = transfersInPeriod
                    .Where(t => accountCurrency.TryGetValue(t.FromAccountId, out var c) && c == g.Key)
                    .Sum(t => t.FromAmount);

                return new PeriodSummaryDto(
                    g.Key,
                    g.Sum(a => a.InitialBalance),
                    income,
                    expenses,
                    transferIn,
                    transferOut,
                    income - expenses + transferIn - transferOut);
            })
            .ToList();

        return new DashboardSummaryResponse(summaries);
    }

    public async Task<CategoryAnalysisResponse> GetCategoryAnalysisAsync(DateRange? period = null, CancellationToken ct = default) {
        var transactions = await Db.Table<TransactionEntity>().ToListAsync().ConfigureAwait(false);
        var categories = await Db.Table<CategoryEntity>().ToListAsync().ConfigureAwait(false);

        var (from, to) = period is { } p
            ? (p.From.UtcDateTime, p.To.UtcDateTime)
            : (DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime());

        var inPeriod = transactions.Where(t => t.TransactionDateUtc >= from && t.TransactionDateUtc <= to).ToList();

        var breakdown = inPeriod
            .Where(t => t.CategoryId is not null)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new CategoryBreakdownDto(
                g.Key,
                categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "—",
                (TransactionType)g.First().TransactionType,
                g.GroupBy(t => t.Currency)
                    .Select(c => new CurrencyAmountDto(c.Key, c.Sum(t => t.Amount), c.Count()))
                    .ToList()))
            .OrderByDescending(b => b.CurrencyBreakdown.Sum(c => c.TotalAmount))
            .ToList();

        return new CategoryAnalysisResponse(breakdown);
    }

    // ---- Query helpers -------------------------------------------------------

    private async Task<List<TransactionEntity>> QueryTransactions(TransactionQuery query) {
        var rows = await Db.Table<TransactionEntity>().ToListAsync().ConfigureAwait(false);

        IEnumerable<TransactionEntity> result = rows;

        if (query.AccountId is { } accountId) {
            result = result.Where(t => t.AccountId == accountId);
        }
        if (query.CategoryId is { } categoryId) {
            result = result.Where(t => t.CategoryId == categoryId);
        }
        if (query.TransactionType is { } type) {
            result = result.Where(t => (TransactionType)t.TransactionType == type);
        }
        if (query.DateFrom is { } from) {
            result = result.Where(t => t.TransactionDateUtc >= from.UtcDateTime);
        }
        if (query.DateTo is { } to) {
            result = result.Where(t => t.TransactionDateUtc <= to.UtcDateTime);
        }
        if (!string.IsNullOrWhiteSpace(query.Search)) {
            var search = query.Search.Trim();
            result = result.Where(t =>
                (t.Note?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return [.. result
            .OrderByDescending(t => t.TransactionDateUtc)
            .ThenByDescending(t => t.CreatedAtUtc)];
    }

    private static List<TransactionEntity> Paginate(List<TransactionEntity> rows, int page, int size)
        => [.. rows.Skip((page - 1) * size).Take(size)];

    private static int TotalPages(int count, int size) => size <= 0 ? 1 : (int)Math.Ceiling(count / (double)size);

    private static IEnumerable<TransactionEntity> ExpenseTransactionRows(List<TransactionEntity> rows, DateTime monthStart)
        => rows.Where(t => IsExpenseType((TransactionType)t.TransactionType) && t.TransactionDateUtc >= monthStart);

    private static bool IsIncomeType(TransactionType type)
        => type is TransactionType.Income or TransactionType.TransferIn;

    private static bool IsExpenseType(TransactionType type)
        => type is TransactionType.Expense or TransactionType.TransferOut;

    private static decimal SignedDelta(TransactionType type, decimal amount)
        => IsIncomeType(type) ? amount : -amount;

    private async Task<AccountEntity> RequireAccountAsync(Guid id)
        => await Db.FindAsync<AccountEntity>(id).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Account {id} not found.");

    private async Task AdjustAccountBalanceAsync(Guid accountId, decimal delta) {
        if (delta == 0) {
            return;
        }

        await Db.ExecuteAsync(
            "UPDATE accounts SET CurrentBalance = CurrentBalance + ?, UpdatedAtUtc = ? WHERE Id = ?",
            delta, DateTime.UtcNow, accountId).ConfigureAwait(false);
    }

    private async Task<Dictionary<Guid, string>> BuildNameLookupAsync() {
        var accounts = await Db.Table<AccountEntity>().ToListAsync().ConfigureAwait(false);
        return accounts.ToDictionary(a => a.Id, a => a.Name);
    }

    private async Task<Dictionary<Guid, (string name, Currency currency)>> BuildTransferLookupAsync() {
        var accounts = await Db.Table<AccountEntity>().ToListAsync().ConfigureAwait(false);
        return accounts.ToDictionary(a => a.Id, a => (a.Name, a.Currency));
    }

    // ---- Mapping -------------------------------------------------------------

    private static AccountDto ToDto(AccountEntity e)
        => new(e.Id, e.Name, e.InitialBalance, e.CurrentBalance, e.Currency);

    private static AccountMutationResult ToMutationResult(AccountEntity e)
        => new(e.Id, e.Name, e.InitialBalance, e.Currency);

    private static CategoryDto ToDto(CategoryEntity e)
        => new(e.Id, e.Name, e.TransactionType is { } t ? (TransactionType)t : null);

    private static TransactionDto ToDto(TransactionEntity e, Dictionary<Guid, string> names)
        => new(
            e.Id,
            e.AccountId,
            names.TryGetValue(e.AccountId, out var accountName) ? accountName : "—",
            e.CategoryId,
            null,
            (TransactionType)e.TransactionType,
            e.Currency,
            e.Amount,
            e.Note,
            new DateTimeOffset(e.TransactionDateUtc, TimeSpan.Zero),
            new DateTimeOffset(e.CreatedAtUtc, TimeSpan.Zero));

    private static DashboardTransactionDto ToDashboardDto(TransactionEntity e, Dictionary<Guid, string> categories, Dictionary<Guid, string> accounts)
        => new(
            e.Id,
            e.AccountId,
            accounts.TryGetValue(e.AccountId, out var accountName) ? accountName : "—",
            e.Currency,
            e.CategoryId,
            e.CategoryId is { } c && categories.TryGetValue(c, out var categoryName) ? categoryName : null,
            (TransactionType)e.TransactionType,
            e.Amount,
            e.Note,
            new DateTimeOffset(e.TransactionDateUtc, TimeSpan.Zero));

    private static TransferDto ToDto(TransferEntity e, Dictionary<Guid, (string name, Currency currency)> names)
        => new(
            e.Id,
            e.FromAccountId,
            names.TryGetValue(e.FromAccountId, out var from) ? from.name : "—",
            from.currency,
            e.ToAccountId,
            names.TryGetValue(e.ToAccountId, out var to) ? to.name : "—",
            to.currency,
            e.FromAmount,
            e.ToAmount,
            e.Note,
            new DateTimeOffset(e.TransferDateUtc, TimeSpan.Zero),
            new DateTimeOffset(e.CreatedAtUtc, TimeSpan.Zero));
}