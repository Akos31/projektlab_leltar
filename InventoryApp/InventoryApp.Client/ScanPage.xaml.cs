using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client
{
    public partial class ScanPage : ContentPage
    {
        private readonly ApiService _api;
        private bool _loading;
        private int _sessionScans;

        private ZoneItem? Zone => ZonePicker.SelectedItem as ZoneItem;
        private RoomItem? Room => RoomPicker.SelectedItem as RoomItem;

        public ScanPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadZonesAsync();
        }

        private async Task LoadZonesAsync()
        {
            try
            {
                int? keep = Zone?.Id;
                _loading = true;
                var zones = await _api.GetZonesAsync();
                ZonePicker.ItemsSource = zones;
                if (keep != null)
                    ZonePicker.SelectedItem = zones.FirstOrDefault(z => z.Id == keep);
                _loading = false;
                StatusLabel.Text = zones.Count == 0 ? "Nincs körzet, előbb importálj Excelt." : "";
                await LoadRoomsAsync();
            }
            catch (Exception ex)
            {
                _loading = false;
                StatusLabel.Text = $"A szerver nem érhető el: {ex.Message}";
            }
        }

        private async Task LoadRoomsAsync(int? selectId = null)
        {
            RoomPicker.ItemsSource = null;
            if (Zone != null)
            {
                var rooms = await _api.GetRoomsAsync(Zone.Id);
                RoomPicker.ItemsSource = rooms;
                if (selectId != null)
                    RoomPicker.SelectedItem = rooms.FirstOrDefault(r => r.Id == selectId);
            }
            UpdateState();
        }

        private void UpdateState()
        {
            var zone = Zone;
            PeriodLabel.Text = zone == null ? ""
                : zone.OpenPeriodId == null ? "Nincs nyitott időszak"
                : $"Nyitott időszak: #{zone.OpenPeriodId}";
            OpenPeriodButton.IsEnabled = zone != null && zone.OpenPeriodId == null;
            ClosePeriodButton.IsEnabled = zone?.OpenPeriodId != null;

            bool ready = zone?.OpenPeriodId != null && Room != null;
            CodeEntry.IsEnabled = ready;
            ScanButton.IsEnabled = ready;
        }

        private async void OnZoneChanged(object? sender, EventArgs e)
        {
            if (_loading) return;
            try { await LoadRoomsAsync(); }
            catch (Exception ex) { StatusLabel.Text = $"Hiba: {ex.Message}"; }
        }

        private void OnRoomChanged(object? sender, EventArgs e) => UpdateState();

        private async void OnOpenPeriod(object? sender, EventArgs e)
        {
            if (Zone == null) return;
            try
            {
                var period = await _api.OpenPeriodAsync(Zone.Id);
                Zone.OpenPeriodId = period?.Id;
                UpdateState();
            }
            catch (Exception ex) { StatusLabel.Text = $"Hiba: {ex.Message}"; }
        }
        private async void OnClosePeriod(object? sender, EventArgs e)
        {
            if (Zone == null || Zone.OpenPeriodId == null) return;

            bool ok = await DisplayAlert("Időszak lezárása",
                $"Biztosan lezárod a(z) {Zone.Display} körzet nyitott időszakát? " +
                "Lezárás után ebben a körzetben nem lehet beolvasni.",
                "Lezár", "Mégse");
            if (!ok) return;

            try
            {
                await _api.ClosePeriodAsync(Zone.Id);
                Zone.OpenPeriodId = null;
                UpdateState();
                ResultCard.IsVisible = false;
            }
            catch (Exception ex) { StatusLabel.Text = $"Hiba: {ex.Message}"; }
        }

        private async void OnAddRoom(object? sender, EventArgs e)
        {
            var number = NewRoomEntry.Text?.Trim();
            if (Zone == null || string.IsNullOrEmpty(number)) return;
            try
            {
                var room = await _api.AddRoomAsync(Zone.Id, number);
                NewRoomEntry.Text = "";
                await LoadRoomsAsync(room?.Id);
            }
            catch (Exception ex) { StatusLabel.Text = $"Hiba: {ex.Message}"; }
        }

        private async void OnScan(object? sender, EventArgs e)
        {
            var code = CodeEntry.Text?.Trim();
            if (string.IsNullOrEmpty(code) || Zone == null || Room == null) return;

            try
            {
                var r = await _api.ScanAsync(code, Zone.Id, Room.Id);
                switch (r.Result)
                {
                    case "Ok":
                        _sessionScans++;
                        ShowResult($"✔ {r.Asset?.Name} – beolvasva ({r.ScannedTotal}/{r.ExpectedQuantity})", "#2E7D32");
                        break;
                    case "OtherZone":
                        _sessionScans++;
                        ShowResult($"⚠ {r.Asset?.Name} másik körzet eszköze – elmentve, megjelölve", "#EF6C00");
                        break;
                    default:
                        ShowResult($"✖ {r.Message}", "#C62828");
                        break;
                }
                StatusLabel.Text = $"Ebben a munkamenetben: {_sessionScans} sikeres beolvasás";
            }
            catch (Exception ex)
            {
                ShowResult($"✖ A szerver nem érhető el: {ex.Message}", "#C62828");
            }

            CodeEntry.Text = "";
            CodeEntry.Focus();
        }

        private void ShowResult(string text, string colorHex)
        {
            ResultLabel.Text = text;
            ResultCard.BackgroundColor = Color.FromArgb(colorHex);
            ResultCard.IsVisible = true;
        }
    }
}