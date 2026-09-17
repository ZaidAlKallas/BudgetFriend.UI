using BudgetFriend.App.Application.Services;

namespace BudgetFriend.Web.Services;

/// <summary>The web app is always assumed online.</summary>
public sealed class WebConnectivityService : IConnectivityService {
    public bool IsConnected => true;

    public event EventHandler<bool>? ConnectivityChanged;
}