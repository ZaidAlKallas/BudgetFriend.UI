using BudgetFriend.App.Application.Services;
using System.Collections.Concurrent;

namespace BudgetFriend.Web.Services;

/// <summary>
/// In-memory server-side session vault. Token material never reaches the
/// browser; the browser only holds an opaque httpOnly session cookie that
/// keys into this vault, so client-side script (XSS) cannot read the access
/// or refresh tokens. Idle entries are evicted periodically.
/// </summary>
public sealed class ServerSessionVault {
    private static readonly TimeSpan MaxIdle = TimeSpan.FromDays(7);

    private readonly ConcurrentDictionary<Guid, Entry> _sessions = new();

    private sealed record Entry(TokenSession Session, DateTimeOffset? LastAccessedUtc);

    public bool Contains(Guid id) => _sessions.ContainsKey(id);

    public TokenSession? TryGet(Guid id) {
        if (!_sessions.TryGetValue(id, out var entry)) {
            return null;
        }

        _sessions[id] = entry with { LastAccessedUtc = DateTimeOffset.UtcNow };
        return entry.Session;
    }

    public void Put(Guid id, TokenSession session) {
        _sessions[id] = new Entry(session, DateTimeOffset.UtcNow);
        EvictExpired();
    }

    public void Remove(Guid id) {
        _sessions.TryRemove(id, out _);
    }

    private void EvictExpired() {
        var cutoff = DateTimeOffset.UtcNow - MaxIdle;
        foreach (var pair in _sessions) {
            if (pair.Value.LastAccessedUtc is { } accessed && accessed < cutoff) {
                _sessions.TryRemove(pair.Key, out _);
            }
        }
    }
}