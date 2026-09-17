namespace BudgetFriend.App.Application.Services;

/// <summary>
/// Stores the current authentication session. Concrete implementations are
/// platform specific (browser storage for Web, secure storage for Mobile).
/// </summary>
public interface ITokenStore {
    Task<TokenSession?> GetAsync();
    Task SetAsync(TokenSession session);
    Task ClearAsync();
}

/// <summary>Authentication tokens persisted between app runs.</summary>
public sealed record TokenSession(string AccessToken, string RefreshToken, DateTimeOffset ExpireAtUtc);