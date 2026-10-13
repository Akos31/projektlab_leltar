using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client
{
    public partial class ZonesPage : ContentPage
    {
        private readonly ApiService _api;
        private ZoneItem? _editing;

        public ZonesPage(ApiService api)
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
                var zones = await _api.GetZonesAsync(true);
                ZoneList.ItemsSource = zones;
                CountLabel.Text = $"{zones.Count} körzet, ebből {zones.Count(z => z.IsActive)} aktív";
            }
            catch (Exception ex)
            {
                ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
            }
        }

        private async void OnSave(object? sender, EventArgs e)
        {
            var code = CodeEntry.Text?.Trim() ?? "";
            var name = NameEntry.Text?.Trim() ?? "";
            try
            {
                var (ok, error) = await _api.SaveZoneAsync(_editing?.Id, code, name);
                if (!ok) { ShowMessage(error ?? "Hiba a mentéskor.", error: true); return; }

                ResetForm();
                ShowMessage("Mentve.", error: false);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
            }
        }

        private void OnEdit(object? sender, EventArgs e)
        {
            if ((sender as Button)?.BindingContext is not ZoneItem zone) return;
            _editing = zone;
            CodeEntry.Text = zone.Code;
            NameEntry.Text = zone.Name;
            FormTitle.Text = $"Körzet szerkesztése: {zone.Code}";
            CancelButton.IsVisible = true;
            MessageLabel.Text = "";
        }

        private void OnCancel(object? sender, EventArgs e) => ResetForm();

        private async void OnToggle(object? sender, EventArgs e)
        {
            if ((sender as Button)?.BindingContext is not ZoneItem zone) return;

            if (zone.IsActive)
            {
                bool ok = await DisplayAlert("Inaktívvá tétel",
                    $"Inaktívvá teszed a(z) {zone.FullName} körzetet? " +
                    "A korábbi beolvasások megmaradnak, a körzet később újra aktiválható.",
                    "Inaktívvá tesz", "Mégse");
                if (!ok) return;
            }

            try
            {
                var (success, error) = await _api.SetZoneActiveAsync(zone.Id, !zone.IsActive);
                if (!success) { ShowMessage(error ?? "Hiba.", error: true); return; }
                ShowMessage("Állapot módosítva.", error: false);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
            }
        }

        private void ResetForm()
        {
            _editing = null;
            CodeEntry.Text = "";
            NameEntry.Text = "";
            FormTitle.Text = "Új körzet";
            CancelButton.IsVisible = false;
        }

        private void ShowMessage(string text, bool error)
        {
            MessageLabel.Text = text;
            MessageLabel.TextColor = error ? Color.FromArgb("#C62828") : Color.FromArgb("#2E7D32");
        }
    }
}