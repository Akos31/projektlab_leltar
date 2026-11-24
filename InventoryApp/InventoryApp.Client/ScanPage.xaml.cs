using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client
{
    public partial class ScanPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly SoundService _sound;
        private bool _loading;
        private int _sessionScans;

        private ZoneItem? Zone => ZonePicker.SelectedItem as ZoneItem;
        private RoomItem? Room => RoomPicker.SelectedItem as RoomItem;

        public ScanPage(ApiService api, RealtimeService realtime, SoundService sound)
        {
            InitializeComponent();
            _api = api;
            _sound = sound;
            SoundSwitch.IsToggled = _sound.IsEnabled;

            realtime.ScanCreated += OnLiveScan;
            realtime.PeriodChanged += OnLivePeriodChanged;
        }

        private void OnLiveScan(object? sender, ScanItem scan)
        {
            if (Zone == null || scan.ZoneId != Zone.Id) return;

            LiveLabel.Text = $"Legutóbb a körzetben: {scan.Title} · {scan.Room} · " +
                             $"{scan.ScannedQuantity} db · {scan.Timestamp.ToLocalTime():HH:mm:ss}";
            LiveLabel.IsVisible = true;
        }

        private void OnLivePeriodChanged(object? sender, PeriodChangedMessage message)
        {
            if (ZonePicker.ItemsSource is not IEnumerable<ZoneItem> zones) return;

            var zone = zones.FirstOrDefault(z => z.Id == message.ZoneId);
            if (zone == null || zone.OpenPeriodId == message.OpenPeriodId) return;

            zone.OpenPeriodId = message.OpenPeriodId;
            if (zone != Zone) return;

            UpdateState();
            LiveLabel.Text = message.OpenPeriodId == null
                ? $"A(z) {zone.Display} körzet időszakát lezárták, itt már nem lehet beolvasni."
                : $"A(z) {zone.Display} körzetben elindult a #{message.OpenPeriodId} időszak.";
            LiveLabel.IsVisible = true;
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
            QuantityStepper.IsEnabled = ready;
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

            var quantity = (int)QuantityStepper.Value;

            try
            {
                var r = await _api.ScanAsync(code, Zone.Id, Room.Id, quantity);
                switch (r.Result)
                {
                    case "Ok":
                        _sessionScans++;
                        if (r.ScannedTotal > r.ExpectedQuantity)
                        {
                            ShowResult($"⚠ {r.Asset?.Name} – TÖBBLET ({r.ScannedTotal}/{r.ExpectedQuantity})", "#EF6C00");
                            _sound.Play(ScanSound.Warning);
                        }
                        else
                        {
                            ShowResult($"✔ {r.Asset?.Name} – beolvasva ({r.ScannedTotal}/{r.ExpectedQuantity})", "#2E7D32");
                            _sound.Play(ScanSound.Success);
                        }
                        break;
                    case "OtherZone":
                        _sessionScans++;
                        ShowResult($"⚠ {r.Asset?.Name} másik körzet eszköze – elmentve, megjelölve", "#EF6C00");
                        _sound.Play(ScanSound.Warning);
                        break;
                    default:
                        ShowResult($"✖ {r.Message}", "#C62828");
                        _sound.Play(ScanSound.Error);
                        break;
                }
                StatusLabel.Text = $"Ebben a munkamenetben: {_sessionScans} sikeres beolvasás";
            }
            catch (Exception ex)
            {
                ShowResult($"✖ A szerver nem érhető el: {ex.Message}", "#C62828");
                _sound.Play(ScanSound.Error);
            }

            CodeEntry.Text = "";
            QuantityStepper.Value = 1;
            CodeEntry.Focus();
        }

        private void OnQuantityChanged(object? sender, ValueChangedEventArgs e)
        {
            QuantityLabel.Text = ((int)e.NewValue).ToString();
        }

        private void OnSoundToggled(object? sender, ToggledEventArgs e)
        {
            if (e.Value == _sound.IsEnabled)
                return;   // a mentett beállítás betöltése ne szólaljon meg

            _sound.IsEnabled = e.Value;
            if (e.Value)
                _sound.Play(ScanSound.Success);
        }

        private void ShowResult(string text, string colorHex)
        {
            ResultLabel.Text = text;
            ResultCard.BackgroundColor = Color.FromArgb(colorHex);
            ResultCard.IsVisible = true;
        }
    }
}