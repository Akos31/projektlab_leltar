using System.Collections.ObjectModel;
using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class AssetStatusPage : ContentPage
{
    private static readonly (string Value, string Label)[] StatusOptions =
    {
        ("active", "Aktív"),
        ("decommissioned", "Kivonva"),
        ("lost", "Elveszett"),
        ("stolen", "Ellopva"),
        ("other", "Egyéb"),
    };

    private readonly ApiService _api;
    private readonly int _assetId;
    private readonly ObservableCollection<AssetStatusHistoryEntry> _history = new();

    public AssetStatusPage(ApiService api, UserSession session, int assetId, string assetName, string currentStatus)
    {
        InitializeComponent();
        _api = api;
        _assetId = assetId;

        AssetNameLabel.Text = assetName;
        CurrentStatusLabel.Text = $"Jelenlegi állapot: {AssetSummary.ToStatusText(currentStatus)}";

        StatusPicker.ItemsSource = StatusOptions.Select(o => o.Label).ToList();
        var currentIndex = Array.FindIndex(StatusOptions, o => o.Value == currentStatus);
        StatusPicker.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;

        EditPanel.IsVisible = session.CanManageAssetStatus;

        HistoryList.ItemsSource = _history;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _api.GetAssetStatusHistoryAsync(_assetId);
            _history.Clear();
            foreach (var entry in history)
                _history.Add(entry);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Nem sikerült betölteni az előzményeket: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selected = StatusOptions[StatusPicker.SelectedIndex];
        ErrorLabel.IsVisible = false;

        try
        {
            var result = await _api.UpdateAssetStatusAsync(_assetId, selected.Value);
            CurrentStatusLabel.Text = $"Jelenlegi állapot: {AssetSummary.ToStatusText(result.Status)}";
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Nem sikerült menteni: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e) => await Navigation.PopModalAsync();
}