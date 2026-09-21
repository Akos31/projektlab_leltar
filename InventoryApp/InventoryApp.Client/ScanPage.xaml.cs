using System.Collections.ObjectModel;
using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class ScanPage : ContentPage
{
    private readonly ApiService _api;
    public ObservableCollection<AssetSummary> Assets { get; } = new();

    public ScanPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        AssetsList.ItemsSource = Assets;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAssetsAsync();
    }

    private async Task LoadAssetsAsync()
    {
        try
        {
            StatusLabel.Text = "Betöltés...";
            var assets = await _api.GetAssetsAsync();

            Assets.Clear();
            foreach (var asset in assets)
                Assets.Add(asset);

            StatusLabel.Text = $"{Assets.Count} eszköz betöltve";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Hiba történt: {ex.Message}";
        }
    }
}