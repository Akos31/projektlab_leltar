using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class AssetTypesPage : ContentPage
{
    private readonly ApiService _api;

    public AssetTypesPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var types = await _api.GetAssetTypesAsync();
            TypeList.ItemsSource = types;
            CountLabel.Text = $"{types.Count} eszköztípus";
        }
        catch (Exception ex)
        {
            ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
        }
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? "";
        try
        {
            var (ok, error) = await _api.CreateAssetTypeAsync(name);
            if (!ok) { ShowMessage(error ?? "Hiba a mentéskor.", error: true); return; }

            NameEntry.Text = "";
            ShowMessage("Mentve.", error: false);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
        }
    }

    private void ShowMessage(string text, bool error)
    {
        MessageLabel.Text = text;
        MessageLabel.TextColor = error ? Color.FromArgb("#C62828") : Color.FromArgb("#2E7D32");
    }
}