using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class AssetsPage : ContentPage
{
    private const string All = "Összes";
    private const double MinCardWidth = 380; // ennél keskenyebb kártya nem lesz -> oszlopszám

    private readonly ApiService _api;
    private List<AssetSummary> _allAssets = new();
    private bool _loaded;
    private bool _suppressFilter;
    private CancellationTokenSource? _searchCts;

    public AssetsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Csak először töltünk be automatikusan, így fülváltáskor megmaradnak a szűrők.
        if (!_loaded)
            await LoadAssetsAsync();
    }

    private async Task LoadAssetsAsync()
    {
        SetBusy(true);
        try
        {
            _allAssets = await _api.GetAssetsAsync();
            _loaded = true;

            _suppressFilter = true;
            FillPicker(StatusPicker, a => a.StatusText);
            FillPicker(TypePicker, a => a.DisplayType);
            FillPicker(ZonePicker, a => a.Zone);
            _suppressFilter = false;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Nem sikerült betölteni az eszközöket: {ex.Message}";
        }
        finally
        {
            _suppressFilter = false;
            SetBusy(false);
            RefreshContainer.IsRefreshing = false;
        }
    }

    private void SetBusy(bool busy)
    {
        LoadingIndicator.IsVisible = busy;
        LoadingIndicator.IsRunning = busy;
        if (busy)
            ResultLabel.Text = "Betöltés...";
    }

    // "Összes" + a letöltött adatokban szereplő egyedi értékek, ábécérendben.
    // Frissítéskor megtartja a korábbi választást, ha az még létezik.
    private void FillPicker(Picker picker, Func<AssetSummary, string?> selector)
    {
        var previous = picker.SelectedItem as string;

        var items = new List<string> { All };
        items.AddRange(_allAssets
            .Select(selector)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .Distinct()
            .OrderBy(v => v));

        picker.ItemsSource = items;
        picker.SelectedItem = previous is not null && items.Contains(previous) ? previous : All;
    }

    private void ApplyFilters()
    {
        if (_suppressFilter)
            return;

        var search = SearchEntry.Text?.Trim() ?? string.Empty;

        IEnumerable<AssetSummary> query = _allAssets;

        if (search.Length > 0)
        {
            query = query.Where(a =>
                a.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.SourceId.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        query = FilterBy(query, StatusPicker, a => a.StatusText);
        query = FilterBy(query, TypePicker, a => a.DisplayType);
        query = FilterBy(query, ZonePicker, a => a.Zone);

        // Egyben cseréljük a listát (gyorsabb, mint 1800 elemet egyesével hozzáadni).
        var filtered = query.ToList();
        AssetsList.ItemsSource = filtered;

        var hasFilter = search.Length > 0
                        || IsFiltered(StatusPicker)
                        || IsFiltered(TypePicker)
                        || IsFiltered(ZonePicker);

        ResultLabel.Text = hasFilter
            ? $"{filtered.Count} találat ({_allAssets.Count} eszközből)"
            : $"{_allAssets.Count} eszköz";

        ResetButton.IsVisible = hasFilter;
    }

    private static bool IsFiltered(Picker picker) =>
        picker.SelectedItem is string s && s != All;

    private static IEnumerable<AssetSummary> FilterBy(
        IEnumerable<AssetSummary> query, Picker picker, Func<AssetSummary, string?> selector)
    {
        if (!IsFiltered(picker))
            return query;

        var selected = (string)picker.SelectedItem;
        return query.Where(a => selector(a) == selected);
    }

    // Gépelés közben csak 250 ms szünet után szűrünk, hogy ne akadjon.
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(250, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        ApplyFilters();
    }

    private void OnFilterChanged(object sender, EventArgs e) => ApplyFilters();

    private void OnResetClicked(object sender, EventArgs e)
    {
        _suppressFilter = true;
        SearchEntry.Text = string.Empty;
        StatusPicker.SelectedItem = All;
        TypePicker.SelectedItem = All;
        ZonePicker.SelectedItem = All;
        _suppressFilter = false;

        ApplyFilters();
    }

    private async void OnRefreshing(object sender, EventArgs e) => await LoadAssetsAsync();

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadAssetsAsync();

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