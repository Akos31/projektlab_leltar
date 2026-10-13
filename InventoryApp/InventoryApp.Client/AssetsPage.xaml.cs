using System.Collections.ObjectModel;
using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class AssetsPage : ContentPage
{
    private const int PageSize = 100;
    private const double MinCardWidth = 380; // ennél keskenyebb kártya nem lesz -> oszlopszám

    private static readonly FilterOption AllOption = new(null, "Összes");

    private readonly ApiService _api;
    private readonly ObservableCollection<AssetSummary> _assets = new();

    private int _totalCount;          // a szűrésnek megfelelő összes eszköz a szerveren
    private bool _isLoading;
    private bool _loaded;
    private bool _autoLoadBlocked;    // hiba után ne próbálkozzon görgetésre végtelenül
    private bool _suppressFilter;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource _pageCts = new();

    private bool HasMore => _assets.Count < _totalCount;

    public AssetsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        AssetsList.ItemsSource = _assets;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Csak először töltünk be automatikusan, így fülváltáskor megmaradnak a szűrők.
        if (!_loaded)
            await RefreshAllAsync();
    }

    // Szűrőértékek újratöltése + lista elölről
    private async Task RefreshAllAsync()
    {
        try
        {
            var filters = await _api.GetAssetFiltersAsync();

            _suppressFilter = true;
            FillPicker(StatusPicker, filters.Statuses.Select(s => new FilterOption(s, AssetSummary.ToStatusText(s))));
            FillPicker(TypePicker, filters.Types.Select(t => new FilterOption(t, AssetSummary.ToDisplayType(t))));
            FillPicker(ZonePicker, filters.Zones.Select(z => new FilterOption(z, z)));
            _loaded = true;
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Nem sikerült betölteni a szűrőket: {ex.Message}";
            RefreshContainer.IsRefreshing = false;
            return;
        }
        finally
        {
            _suppressFilter = false;
        }

        await LoadPageAsync(reset: true);
    }

    // reset = true  -> új szűrés, a lista elölről indul
    // reset = false -> a következő 100 elem hozzáfűzése
    private async Task LoadPageAsync(bool reset)
    {
        if (reset)
        {
            // Ha még fut egy korábbi szűrés lekérése, azt eldobjuk
            _pageCts.Cancel();
            _pageCts = new CancellationTokenSource();
            _assets.Clear();
            _totalCount = 0;
            _autoLoadBlocked = false;
        }
        else if (_isLoading || !HasMore)
        {
            return;
        }

        var cts = _pageCts;
        _isLoading = true;
        UpdateLoadingUi(firstPage: reset);

        try
        {
            var page = await _api.GetAssetsPageAsync(_assets.Count, PageSize, BuildQuery(), cts.Token);
            if (cts.IsCancellationRequested)
                return;

            foreach (var asset in page.Items)
                _assets.Add(asset);

            _totalCount = page.TotalCount;
            UpdateResultLabel();
        }
        catch (OperationCanceledException)
        {
            // Közben új szűrés indult - nem hiba.
        }
        catch (Exception ex)
        {
            if (!cts.IsCancellationRequested)
            {
                _autoLoadBlocked = true;
                ResultLabel.Text = $"Nem sikerült betölteni az eszközöket: {ex.Message}";
            }
        }
        finally
        {
            // Csak az aktuális (nem eldobott) lekérés állítja vissza az állapotot
            if (cts == _pageCts)
            {
                _isLoading = false;
                UpdateLoadingUi(firstPage: reset);
                RefreshContainer.IsRefreshing = false;
            }
        }
    }

    private void UpdateLoadingUi(bool firstPage)
    {
        var showTop = _isLoading && firstPage;
        LoadingIndicator.IsVisible = showTop;
        LoadingIndicator.IsRunning = showTop;

        var showBottom = _isLoading && !firstPage;
        LoadMoreIndicator.IsVisible = showBottom;
        LoadMoreIndicator.IsRunning = showBottom;

        LoadMoreButton.IsVisible = !_isLoading && HasMore;

        if (showTop)
            ResultLabel.Text = "Betöltés...";
    }

    private void UpdateResultLabel()
    {
        var hasFilter = HasActiveFilter();

        if (_totalCount == 0)
            ResultLabel.Text = hasFilter ? "Nincs találat" : "Nincs eszköz";
        else if (hasFilter)
            ResultLabel.Text = $"{_totalCount} találat ({_assets.Count} megjelenítve)";
        else
            ResultLabel.Text = $"{_totalCount} eszköz ({_assets.Count} megjelenítve)";

        ResetButton.IsVisible = hasFilter;
    }

    // "Összes" + a szervertől kapott értékek. Frissítéskor megtartja a korábbi választást.
    private static void FillPicker(Picker picker, IEnumerable<FilterOption> options)
    {
        var previous = picker.SelectedItem as FilterOption;

        var items = new List<FilterOption> { AllOption };
        items.AddRange(options);

        picker.ItemsSource = items;
        picker.SelectedItem = previous is not null && items.Contains(previous) ? previous : AllOption;
    }

    private AssetQuery BuildQuery() => new()
    {
        Search = SearchEntry.Text?.Trim(),
        Status = SelectedValue(StatusPicker),
        Type = SelectedValue(TypePicker),
        Zone = SelectedValue(ZonePicker)
    };

    private static string? SelectedValue(Picker picker) =>
        (picker.SelectedItem as FilterOption)?.Value;

    private bool HasActiveFilter()
    {
        var q = BuildQuery();
        return !string.IsNullOrWhiteSpace(q.Search) || q.Status is not null || q.Type is not null || q.Zone is not null;
    }

    // --- Eseménykezelők ---

    // Görgetéskor, amikor 10 elem van hátra a lista végéig
    private async void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
    {
        if (_autoLoadBlocked)
            return;

        await LoadPageAsync(reset: false);
    }

    // Tartalék gomb a lista alján (és hiba utáni újrapróbálás)
    private async void OnLoadMoreClicked(object sender, EventArgs e)
    {
        _autoLoadBlocked = false;
        await LoadPageAsync(reset: false);
    }

    // Gépelés közben csak 300 ms szünet után kérdezzük le a szervert.
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressFilter)
            return;

        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await LoadPageAsync(reset: true);
    }

    private async void OnFilterChanged(object sender, EventArgs e)
    {
        if (_suppressFilter)
            return;

        await LoadPageAsync(reset: true);
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        _suppressFilter = true;
        SearchEntry.Text = string.Empty;
        StatusPicker.SelectedItem = AllOption;
        TypePicker.SelectedItem = AllOption;
        ZonePicker.SelectedItem = AllOption;
        _suppressFilter = false;

        await LoadPageAsync(reset: true);
    }

    private async void OnRefreshing(object sender, EventArgs e) => await RefreshAllAsync();

    private async void OnRefreshClicked(object sender, EventArgs e) => await RefreshAllAsync();

    // Széles ablakban több oszlopba rendezzük a kártyákat.
    private void OnListSizeChanged(object? sender, EventArgs e)
    {
        if (AssetsList.ItemsLayout is not GridItemsLayout grid || AssetsList.Width <= 0)
            return;

        var span = Math.Max(1, (int)(AssetsList.Width / MinCardWidth));
        if (grid.Span != span)
            grid.Span = span;
    }
}