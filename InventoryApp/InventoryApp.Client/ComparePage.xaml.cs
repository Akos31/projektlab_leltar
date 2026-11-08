using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client
{
    // Két lezárt leltári időszak összehasonlítása (kiértékelő és admin)
    public partial class ComparePage : ContentPage
    {
        private readonly ApiService _api;
        private bool _loading;
        private CompareResult? _result;
        private string _filter = CompareCategories.Changes;

        private CompareZoneOption? Zone => ZonePicker.SelectedItem as CompareZoneOption;

        public ComparePage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPeriodsAsync();
        }

        // Lezárt időszakok betöltése, körzetenként csoportosítva.
        // Minden megjelenéskor frissül, hogy az időközben lezárt leltárak is látszódjanak.
        private async Task LoadPeriodsAsync()
        {
            try
            {
                var keepZone = Zone?.ZoneId;
                var keepA = (PeriodAPicker.SelectedItem as ClosedPeriodItem)?.Id;
                var keepB = (PeriodBPicker.SelectedItem as ClosedPeriodItem)?.Id;

                var periods = await _api.GetClosedPeriodsAsync();
                var zones = periods
                    .GroupBy(p => p.ZoneId)
                    .Select(g => new CompareZoneOption
                    {
                        ZoneId = g.Key,
                        Display = g.First().ZoneDisplay,
                        Periods = g.OrderByDescending(p => p.StartDate).ToList()
                    })
                    .OrderBy(z => z.Display)
                    .ToList();

                _loading = true;
                ZonePicker.ItemsSource = zones;
                ZonePicker.SelectedItem = zones.FirstOrDefault(z => z.ZoneId == keepZone)
                                          ?? zones.FirstOrDefault(z => z.Periods.Count >= 2)
                                          ?? zones.FirstOrDefault();
                _loading = false;

                FillPeriodPickers(keepA, keepB);

                if (zones.Count == 0)
                    MessageLabel.Text = "Még nincs lezárt leltár. Lezárni a Beolvasás fülön lehet.";
            }
            catch (Exception ex)
            {
                _loading = false;
                MessageLabel.Text = $"A szerver nem érhető el: {ex.Message}";
            }
        }

        private void OnZoneChanged(object? sender, EventArgs e)
        {
            if (_loading) return;
            FillPeriodPickers(null, null);
            ClearResult();
        }

        // Alapértelmezés: a két legutóbbi lezárt leltár
        private void FillPeriodPickers(int? keepA, int? keepB)
        {
            var periods = Zone?.Periods ?? new List<ClosedPeriodItem>();
            PeriodAPicker.ItemsSource = periods;
            PeriodBPicker.ItemsSource = periods;

            PeriodBPicker.SelectedItem = periods.FirstOrDefault(p => p.Id == keepB) ?? periods.ElementAtOrDefault(0);
            PeriodAPicker.SelectedItem = periods.FirstOrDefault(p => p.Id == keepA) ?? periods.ElementAtOrDefault(1);

            CompareButton.IsEnabled = periods.Count >= 2;
            MessageLabel.Text = Zone == null ? ""
                : periods.Count < 2 ? "Ebben a körzetben még csak egy lezárt leltár van, legalább kettő kell az összehasonlításhoz."
                : "";
        }

        private async void OnCompare(object? sender, EventArgs e)
        {
            var a = PeriodAPicker.SelectedItem as ClosedPeriodItem;
            var b = PeriodBPicker.SelectedItem as ClosedPeriodItem;
            if (a == null || b == null)
            {
                MessageLabel.Text = "Válassz ki két leltárt.";
                return;
            }
            if (a.Id == b.Id)
            {
                MessageLabel.Text = "Két különböző leltárt válassz.";
                return;
            }

            SetBusy(true);
            try
            {
                var (result, error) = await _api.ComparePeriodsAsync(a.Id, b.Id);
                if (result == null)
                {
                    MessageLabel.Text = error ?? "Nem sikerült az összehasonlítás.";
                    return;
                }

                // A szerver mindig a korábbit adja "A"-nak; ha fordítva választották, jelezzük
                MessageLabel.Text = a.StartDate > b.StartDate
                    ? "A két leltár sorrendjét időrendbe tettük (korábbi → későbbi)."
                    : "";
                ShowResult(result);
            }
            catch (Exception ex)
            {
                MessageLabel.Text = $"A szerver nem érhető el: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ShowResult(CompareResult result)
        {
            _result = result;

            PeriodARange.Text = $"#{result.PeriodA.Id} · {result.PeriodA.Range}";
            PeriodADetail.Text = result.PeriodA.Detail;
            PeriodBRange.Text = $"#{result.PeriodB.Id} · {result.PeriodB.Range}";
            PeriodBDetail.Text = result.PeriodB.Detail;
            PeriodsRow.IsVisible = true;
            FilterArea.IsVisible = true;

            // Ha nincs eltérés, rögtön az összes eszközt mutatjuk
            var s = result.Summary;
            _filter = s.Missing + s.Found + s.Quantity + s.Moved > 0
                ? CompareCategories.Changes
                : CompareCategories.All;

            ApplyFilter();
        }

        private void ClearResult()
        {
            _result = null;
            PeriodsRow.IsVisible = false;
            FilterArea.IsVisible = false;
            ResultList.ItemsSource = null;
            EmptyLabel.Text = "Válassz körzetet és két lezárt leltárt.";
        }

        private void ApplyFilter()
        {
            if (_result == null) return;

            BuildTiles(_result.Summary);

            var search = SearchEntry.Text?.Trim() ?? "";
            var items = _result.Items
                .Where(i => CompareCategories.Matches(_filter, i.ChangeType))
                .Where(i => search.Length == 0 || i.Matches(search))
                .ToList();

            ResultList.ItemsSource = items;
            CountLabel.Text = $"{items.Count} / {_result.Items.Count} eszköz";
            EmptyLabel.Text = _filter == CompareCategories.Changes && search.Length == 0
                ? "Nincs eltérés a két leltár között."
                : "Nincs a szűrésnek megfelelő eszköz.";
        }

        private void BuildTiles(CompareSummary s)
        {
            var tiles = new List<(string Key, int Count)>
            {
                (CompareCategories.Changes, s.Missing + s.Found + s.Quantity + s.Moved),
                ("missing", s.Missing),
                ("found", s.Found),
                ("quantity", s.Quantity),
                ("moved", s.Moved),
                ("notfound", s.NotFound),
                ("unchanged", s.Unchanged),
                (CompareCategories.All, s.Total)
            };

            BindableLayout.SetItemsSource(TilesLayout, tiles.Select(t => new CompareTile
            {
                Key = t.Key,
                Label = CompareCategories.Label(t.Key),
                Count = t.Count,
                Accent = CompareCategories.ColorOf(t.Key),
                IsSelected = t.Key == _filter
            }).ToList());
        }

        private void OnTileTapped(object? sender, TappedEventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not CompareTile tile) return;
            _filter = tile.Key;
            ApplyFilter();
        }

        private void OnSearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilter();

        private void SetBusy(bool busy)
        {
            Busy.IsRunning = busy;
            Busy.IsVisible = busy;
            CompareButton.IsEnabled = !busy;
        }
    }
}