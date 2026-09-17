namespace BudgetFriend.App.Data.Synchronization;

/// <summary>
/// Placeholder for the future synchronization engine. The current BudgetFriend
/// API does not provide sync endpoints, so this boundary is documented only.
///
/// When the API adds support, implementations will:
///   1. Queue local mutations in a <see cref="PendingChange"/> outbox.
///   2. Push local changes to the server.
///   3. Pull server changes and merge locally.
///   4. Resolve conflicts (server-wins or custom strategy).
///   5. Track sync timestamps and pending acknowledgements.
/// </summary>
public interface ISyncService {
    Task SyncAsync(CancellationToken ct = default);
}

/// <summary>Represents a local change that has not yet been sent to the server.</summary>
public sealed class PendingChange {
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Operation { get; init; } = string.Empty; // Create | Update | Delete
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string? SerializedPayload { get; init; }
}