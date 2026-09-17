namespace BudgetFriend.App.Application.Services;

/// <summary>
/// Reports whether the device currently has network access. Web is always
/// considered online; Mobile observes the platform connectivity state.
/// </summary>
public interface IConnectivityService {
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectivityChanged;
}