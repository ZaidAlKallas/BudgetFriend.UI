using BudgetFriend.App.Application.Services;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Tests;

public class OnboardingStateTests {
    [Theory]
    [InlineData(false, false, false, false, false, 0)]
    [InlineData(true, false, false, false, false, 20)]
    [InlineData(true, true, false, false, false, 40)]
    [InlineData(true, true, true, false, false, 60)]
    [InlineData(true, true, true, true, false, 80)]
    [InlineData(true, true, true, true, true, 100)]
    [InlineData(false, true, true, true, false, 0)]
    public void Percent_derives_from_completed_parts(bool reg, bool acc, bool cat, bool tx, bool verified, int expected) {
        var state = new OnboardingState(reg, acc, cat, tx, verified);
        Assert.Equal(expected, state.Percent);
    }

    [Fact]
    public void IsComplete_is_true_only_when_email_is_verified() {
        Assert.True(new OnboardingState(true, true, true, true, true).IsComplete);
        Assert.False(new OnboardingState(true, true, true, true, false).IsComplete);
    }

    [Fact]
    public void IsStepDone_reflects_each_step() {
        var state = new OnboardingState(true, true, false, false, false);
        Assert.True(state.IsStepDone(0));
        Assert.False(state.IsStepDone(1));
        Assert.False(state.IsStepDone(2));
        Assert.False(state.IsStepDone(3));
    }

    [Fact]
    public async Task LoadAsync_combines_profile_accounts_categories_and_preference() {
        var preferences = new FakePreferenceStore();
        var auth = new FakeAuthService {
            Profile = new UserProfile(Guid.NewGuid(), "a@b.c", "Ann", null, IsEmailVerified: false, DateTimeOffset.UtcNow)
        };
        var accounts = new FakeAccountService();
        var categories = new FakeCategoryService();

        var service = new OnboardingService(auth, accounts, categories, preferences);

        var state = await service.LoadAsync();

        Assert.True(state.RegistrationCompleted);
        Assert.False(state.AccountsCompleted);
        Assert.False(state.EmailVerified);
        Assert.Equal(20, state.Percent);
    }

    [Fact]
    public async Task LoadAsync_knowns_created_data_and_verification() {
        var preferences = new FakePreferenceStore();
        var auth = new FakeAuthService {
            Profile = new UserProfile(Guid.NewGuid(), "a@b.c", "Ann", null, IsEmailVerified: true, DateTimeOffset.UtcNow)
        };
        var accounts = new FakeAccountService { Accounts = [new AccountDto(Guid.NewGuid(), "Wallet", 100, 100, Currency.USD)] };
        var categories = new FakeCategoryService { Categories = [new CategoryDto(Guid.NewGuid(), "Food", TransactionType.Expense)] };

        var state = await new OnboardingService(auth, accounts, categories, preferences).LoadAsync();

        Assert.True(state.AccountsCompleted);
        Assert.True(state.CategoriesCompleted);
        Assert.True(state.EmailVerified);
        Assert.Equal(100, state.Percent);
    }

    [Fact]
    public async Task LoadAsync_unauthenticated_user_is_not_started() {
        var auth = new FakeAuthService { Profile = null };

        var state = await new OnboardingService(auth, new FakeAccountService(), new FakeCategoryService(), new FakePreferenceStore()).LoadAsync();

        Assert.False(state.RegistrationCompleted);
        Assert.False(state.IsStarted);
        Assert.Equal(0, state.Percent);
    }

    [Fact]
    public async Task MarkTransactionsCompletedAsync_persists_and_reloads() {
        var preferences = new FakePreferenceStore();
        var auth = new FakeAuthService {
            Profile = new UserProfile(Guid.NewGuid(), "a@b.c", "Ann", null, IsEmailVerified: false, DateTimeOffset.UtcNow)
        };
        var accounts = new FakeAccountService { Accounts = [new AccountDto(Guid.NewGuid(), "Wallet", 100, 100, Currency.USD)] };
        var categories = new FakeCategoryService { Categories = [new CategoryDto(Guid.NewGuid(), "Food", TransactionType.Expense)] };

        var service = new OnboardingService(auth, accounts, categories, preferences);

        var pending = await service.LoadAsync();
        Assert.False(pending.TransactionsCompleted);
        Assert.Equal(60, pending.Percent);

        var done = await service.MarkTransactionsCompletedAsync();
        Assert.True(done.TransactionsCompleted);
        Assert.Equal(80, done.Percent);

        Assert.Equal("1", await preferences.GetAsync(IPreferenceStore.OnboardingTransactionsKey));
    }

    // ---- Fakes --------------------------------------------------------------

    private sealed class FakePreferenceStore : IPreferenceStore {
        private readonly Dictionary<string, string> _values = new();

        public Task<string?> GetAsync(string key) =>
            Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

        public Task SetAsync(string key, string value) {
            _values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAccountService : IAccountService {
        public IReadOnlyList<AccountDto> Accounts { get; set; } = new List<AccountDto>();

        public Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default) => Task.FromResult(Accounts);

        public Task<AccountDto> GetAccountAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Accounts.First(a => a.Id == id));

        public Task<AccountMutationResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default) =>
            Task.FromResult(new AccountMutationResult(Guid.NewGuid(), request.Name, request.InitialBalance, request.Currency));

        public Task<AccountMutationResult> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken ct = default) =>
            Task.FromResult(new AccountMutationResult(id, request.Name, request.InitialBalance, Currency.USD));

        public Task DeleteAccountAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

        public Task<AccountDetailResponse> GetAccountDetailAsync(Guid id, TransactionQuery query, CancellationToken ct = default) =>
            Task.FromResult(new AccountDetailResponse(id, "A", 0, 0, 0, Currency.USD,
                new PagedResult<TransactionDto>([], 1, 20, 0, 0, false, false)));
    }

    private sealed class FakeCategoryService : ICategoryService {
        public IReadOnlyList<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

        public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) => Task.FromResult(Categories);

        public Task<CategoryDto> GetCategoryAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Categories.First(c => c.Id == id));

        public Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default) =>
            Task.FromResult(new CategoryDto(Guid.NewGuid(), request.Name, request.TransactionType));

        public Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default) =>
            Task.FromResult(new CategoryDto(id, request.Name, request.TransactionType));

        public Task DeleteCategoryAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeAuthService : IAuthService {
        public bool IsAuthenticated => Profile is not null;
        public UserProfile? Profile { get; set; }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default) => Task.FromResult(new AuthResult(true));

        public Task<AuthResult> RegisterAsync(string email, string password, string firstName, string? lastName, CancellationToken ct = default) => Task.FromResult(new AuthResult(true));

        public Task<AuthResult> LoginWithGoogleAsync(string idToken, CancellationToken ct = default) => Task.FromResult(new AuthResult(true));

        public Task LogoutAsync(CancellationToken ct = default) {
            Profile = null;
            return Task.CompletedTask;
        }

        public Task<UserProfile?> GetProfileAsync(CancellationToken ct = default) => Task.FromResult(Profile);

        public Task<UserProfile?> RefreshProfileAsync(CancellationToken ct = default) => Task.FromResult(Profile);

        public Task ForgotPasswordAsync(string email, CancellationToken ct = default) => Task.CompletedTask;

        public Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default) => Task.CompletedTask;

        public Task VerifyEmailAsync(string token, CancellationToken ct = default) => Task.CompletedTask;

        public Task ResendVerificationAsync(string email, CancellationToken ct = default) => Task.CompletedTask;
    }
}