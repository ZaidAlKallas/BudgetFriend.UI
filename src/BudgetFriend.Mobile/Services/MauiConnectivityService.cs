using BudgetFriend.App.Application.Services;

namespace BudgetFriend.Mobile.Services;

/// <summary>Observes the device connectivity state via MAUI Connectivity.</summary>
public sealed class MauiConnectivityService : IConnectivityService, IDisposable {
    public MauiConnectivityService() {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e) {
        ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);
    }

    public void Dispose() {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }
}