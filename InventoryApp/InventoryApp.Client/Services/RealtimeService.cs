using InventoryApp.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace InventoryApp.Client.Services;

// SignalR kapcsolat: bejelentkezéskor indul, kijelentkezéskor leáll, magától újrakapcsolódik
public class RealtimeService
{
    private readonly UserSession _session;
    private readonly HubConnection _connection;

    public event EventHandler<ScanItem>? ScanCreated;
    public event EventHandler<PeriodChangedMessage>? PeriodChanged;
    public event EventHandler? StatusChanged;

    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public RealtimeService(HttpClient http, UserSession session)
    {
        _session = session;

        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(http.BaseAddress!, "hubs/inventory"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_session.Current?.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ScanItem>("ScanCreated", scan =>
            OnUi(() => ScanCreated?.Invoke(this, scan)));
        _connection.On<PeriodChangedMessage>("PeriodChanged", message =>
            OnUi(() => PeriodChanged?.Invoke(this, message)));

        _connection.Reconnecting += _ => { OnUi(RaiseStatusChanged); return Task.CompletedTask; };
        _connection.Reconnected += _ => { OnUi(RaiseStatusChanged); return Task.CompletedTask; };
        _connection.Closed += _ => { OnUi(RaiseStatusChanged); return Task.CompletedTask; };

        _session.SignedIn += async (_, _) => await StartAsync();
        _session.SignedOut += async (_, _) => await StopAsync();
        _session.Expired += async (_, _) => await StopAsync();
    }

    private async Task StartAsync()
    {
        if (_connection.State != HubConnectionState.Disconnected)
            return;
        try
        {
            await _connection.StartAsync();
        }
        catch (Exception)
        {
            // élő kapcsolat nélkül a kézi frissítés továbbra is működik
        }
        OnUi(RaiseStatusChanged);
    }

    private async Task StopAsync()
    {
        try
        {
            await _connection.StopAsync();
        }
        catch (Exception)
        {
        }
        OnUi(RaiseStatusChanged);
    }

    private void RaiseStatusChanged() => StatusChanged?.Invoke(this, EventArgs.Empty);

    private static void OnUi(Action action) => MainThread.BeginInvokeOnMainThread(action);
}