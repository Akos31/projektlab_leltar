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
            await LoadPeriodsAsync();
            await RefreshAsync();
        }

        private async Task LoadZonesAsync()
        {
            _loading = true;
            try
            {
                int keep = (ZoneFilter.SelectedItem as ZoneItem)?.Id ?? 0;
                var zones = await _api.GetZonesAsync(true);
                zones.Insert(0, new ZoneItem { Id = 0, Code = "Összes körzet" });
                ZoneFilter.ItemsSource = zones;
                ZoneFilter.SelectedItem = zones.FirstOrDefault(z => z.Id == keep);
            }
            catch (Exception ex)
            {
                CountLabel.Text = $"A szerver nem érhető el: {ex.Message}";
            }
            finally { _loading = false; }
        }

        // Időszakok csak konkrét körzetnél választhatók
        private async Task LoadPeriodsAsync()
        {
            _loading = true;
            try
            {
                var zone = ZoneFilter.SelectedItem as ZoneItem;
                if (zone == null || zone.Id == 0)
                {
                    PeriodFilter.ItemsSource = null;
                    PeriodFilter.IsEnabled = false;
                    return;
                }

                int keep = (PeriodFilter.SelectedItem as PeriodListItem)?.Id ?? 0;
                var periods = await _api.GetPeriodsAsync(zone.Id);
                periods.Insert(0, new PeriodListItem { Id = 0 });
                PeriodFilter.ItemsSource = periods;
                PeriodFilter.SelectedItem = periods.FirstOrDefault(p => p.Id == keep) ?? periods[0];
                PeriodFilter.IsEnabled = true;
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

                var period = PeriodFilter.SelectedItem as PeriodListItem;
                int? periodId = period == null || period.Id == 0 ? null : period.Id;

                bool? other = OtherOnly.IsToggled ? true : null;

                var scans = await _api.GetScansAsync(zoneId, periodId, other);
                ScanList.ItemsSource = scans;
                CountLabel.Text = $"{scans.Count} beolvasás";
            }
            catch (Exception ex)
            {
                CountLabel.Text = $"A szerver nem érhető el: {ex.Message}";
            }
        }

        private async void OnZoneFilterChanged(object? sender, EventArgs e)
        {
            if (_loading) return;
            // körzetváltáskor az időszak-szűrő nullázódik
            PeriodFilter.SelectedItem = null;
            await LoadPeriodsAsync();
            await RefreshAsync();
        }

        private async void OnFilterChanged(object? sender, EventArgs e)
        {
            if (_loading) return;
            await RefreshAsync();
        }

        private async void OnRefresh(object? sender, EventArgs e)
        {
            await LoadPeriodsAsync();   // a darabszámok és az új időszakok frissítéséhez
            await RefreshAsync();
        }
    }
}