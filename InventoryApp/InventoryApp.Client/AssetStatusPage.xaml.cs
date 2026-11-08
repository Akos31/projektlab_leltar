using System.Collections.ObjectModel;
using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class AssetStatusPage : ContentPage
{
    private static readonly (string Value, string Label)[] StatusOptions =
    {
        ("active", "Aktív"),
        ("decommissioned", "Kivonva"),
        ("lost", "Elveszett"),
        ("stolen", "Ellopva"),
        ("other", "Egyéb"),
    };

    private readonly ApiService _api;
    private readonly int _assetId;
    private readonly ObservableCollection<AssetStatusHistoryEntry> _history = new();
    private List<ResponsiblePersonItem> _responsiblePersons = new();
    private string? _currentResponsibleName;

    public AssetStatusPage(ApiService api, UserSession session, int assetId, string assetName, string currentStatus, string? responsiblePersonName)
    {
        InitializeComponent();
        _api = api;
        _assetId = assetId;
        _currentResponsibleName = responsiblePersonName;

        AssetNameLabel.Text = assetName;
        CurrentStatusLabel.Text = $"Jelenlegi állapot: {AssetSummary.ToStatusText(currentStatus)}";

        StatusPicker.ItemsSource = StatusOptions.Select(o => o.Label).ToList();
        var currentIndex = Array.FindIndex(StatusOptions, o => o.Value == currentStatus);
        StatusPicker.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;

        EditPanel.IsVisible = session.CanManageAssetStatus;
        ResponsibleEditPanel.IsVisible = session.CanManageAssetStatus;

        ResponsibleLabel.Text = string.IsNullOrWhiteSpace(responsiblePersonName)
            ? "Jelenleg nincs felelős hozzárendelve."
            : $"Jelenlegi felelős: {responsiblePersonName}";

        HistoryList.ItemsSource = _history;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
        await LoadResponsiblePersonsAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _api.GetAssetStatusHistoryAsync(_assetId);
            _history.Clear();
            foreach (var entry in history)
                _history.Add(entry);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Nem sikerült betölteni az előzményeket: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selected = StatusOptions[StatusPicker.SelectedIndex];
        ErrorLabel.IsVisible = false;

        try
        {
            var result = await _api.UpdateAssetStatusAsync(_assetId, selected.Value);
            CurrentStatusLabel.Text = $"Jelenlegi állapot: {AssetSummary.ToStatusText(result.Status)}";
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Nem sikerült menteni: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }

    private async Task LoadResponsiblePersonsAsync()
    {
        try
        {
            _responsiblePersons = await _api.GetResponsiblePersonsAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Nem sikerült betölteni a felelősök listáját: {ex.Message}";
            ErrorLabel.IsVisible = true;
            _responsiblePersons = new();
        }

        var names = new List<string> { "— Nincs megadva —" };
        names.AddRange(_responsiblePersons.Select(p => p.Name));
        ResponsiblePicker.ItemsSource = names;

        var matchIndex = string.IsNullOrWhiteSpace(_currentResponsibleName)
            ? -1
            : _responsiblePersons.FindIndex(p => p.Name == _currentResponsibleName);
        ResponsiblePicker.SelectedIndex = matchIndex + 1;

        ResponsibleLabel.Text = string.IsNullOrWhiteSpace(_currentResponsibleName)
            ? "Jelenleg nincs felelős hozzárendelve."
            : $"Jelenlegi felelős: {_currentResponsibleName}";
    }

    private async void OnSaveResponsibleClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var newName = NewResponsibleEntry.Text?.Trim();

        try
        {
            int? responsiblePersonId;
            string? responsiblePersonName;

            if (!string.IsNullOrWhiteSpace(newName))
            {
                var (created, error) = await _api.CreateResponsiblePersonAsync(newName);
                if (created is null)
                {
                    ErrorLabel.Text = error ?? "Nem sikerült létrehozni az új felelőst.";
                    ErrorLabel.IsVisible = true;
                    return;
                }
                responsiblePersonId = created.Id;
                responsiblePersonName = created.Name;
            }
            else if (ResponsiblePicker.SelectedIndex > 0)
            {
                var selected = _responsiblePersons[ResponsiblePicker.SelectedIndex - 1];
                responsiblePersonId = selected.Id;
                responsiblePersonName = selected.Name;
            }
            else
            {
                responsiblePersonId = null;
                responsiblePersonName = null;
            }

            var (ok, assignError) = await _api.AssignResponsiblePersonAsync(_assetId, responsiblePersonId);
            if (!ok)
            {
                ErrorLabel.Text = assignError ?? "Nem sikerült menteni.";
                ErrorLabel.IsVisible = true;
                return;
            }

            _currentResponsibleName = responsiblePersonName;
            NewResponsibleEntry.Text = "";
            await LoadResponsiblePersonsAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Nem sikerült menteni: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }
    private async void OnCloseClicked(object sender, EventArgs e) => await Navigation.PopModalAsync();
}