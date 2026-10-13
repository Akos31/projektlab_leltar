using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client
{
    public partial class ScansPage : ContentPage
    {
        private readonly ApiService _api;
        private bool _loading;

        public ScansPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadZonesAsync();
            await RefreshAsync();
        }

        private async Task LoadZonesAsync()
        {
            try
            {
                int? keep = (ZoneFilter.SelectedItem as ZoneItem)?.Id;
                _loading = true;
                var zones = await _api.GetZonesAsync();
                zones.Insert(0, new ZoneItem { Id = 0, Code = "Összes körzet" });
                ZoneFilter.ItemsSource = zones;
                ZoneFilter.SelectedItem = zones.FirstOrDefault(z => z.Id == (keep ?? 0));
            }
            catch (Exception ex)
            {
                CountLabel.Text = $"A szerver nem érhető el: {ex.Message}";
            }
            finally { _loading = false; }
        }

        private async Task RefreshAsync()
        {
            try
            {
                var zone = ZoneFilter.SelectedItem as ZoneItem;
                int? zoneId = zone == null || zone.Id == 0 ? null : zone.Id;
                bool? other = OtherOnly.IsToggled ? true : null;

                var scans = await _api.GetScansAsync(zoneId, other);
                ScanList.ItemsSource = scans;
                CountLabel.Text = $"{scans.Count} beolvasás";
            }
            catch (Exception ex)
            {
                CountLabel.Text = $"A szerver nem érhető el: {ex.Message}";
            }
        }

        private async void OnFilterChanged(object? sender, EventArgs e)
        {
            if (_loading) return;
            await RefreshAsync();
        }

        private async void OnRefresh(object? sender, EventArgs e) => await RefreshAsync();
    }
}