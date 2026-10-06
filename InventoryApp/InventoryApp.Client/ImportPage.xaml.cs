using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class ImportPage : ContentPage
{
    // A backend kötelezőnek veszi a zoneName mezőt, ezért üres helyett ezt küldjük.
    private const string DefaultZoneName = "Importált zóna";

    private static readonly FilePickerFileType ExcelFileType = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, new[] { ".xlsx" } },
            { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
            { DevicePlatform.iOS, new[] { "org.openxmlformats.spreadsheetml.sheet" } },
            { DevicePlatform.MacCatalyst, new[] { "org.openxmlformats.spreadsheetml.sheet" } },
        });

    private readonly ApiService _api;
    private FileResult? _selectedFile;

    public ImportPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnPickFileClicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Válaszd ki az Excel fájlt",
                FileTypes = ExcelFileType
            });

            if (file is null)
                return; // a felhasználó a Mégse gombra kattintott

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Csak .xlsx fájl tölthető fel.");
                return;
            }

            _selectedFile = file;
            FileNameLabel.Text = file.FileName;
            FileInfoLabel.Text = "Készen áll a feltöltésre";
            UploadButton.IsEnabled = true;
            HideMessages();
        }
        catch (Exception ex)
        {
            ShowError($"Nem sikerült megnyitni a fájlt: {ex.Message}");
        }
    }

    private async void OnUploadClicked(object sender, EventArgs e)
    {
        if (_selectedFile is null)
            return;

        HideMessages();
        SetBusy(true);

        try
        {
            var zoneName = string.IsNullOrWhiteSpace(ZoneNameEntry.Text)
                ? DefaultZoneName
                : ZoneNameEntry.Text.Trim();

            await using var stream = await _selectedFile.OpenReadAsync();
            var result = await _api.ImportExcelAsync(stream, _selectedFile.FileName, zoneName);

            ImportedLabel.Text = result.Imported.ToString();
            SkippedLabel.Text = result.SkippedExisting.ToString();
            ResultCard.IsVisible = true;

            ClearSelection();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        BusyPanel.IsVisible = busy;
        PickButton.IsEnabled = !busy;
        UploadButton.IsEnabled = !busy && _selectedFile is not null;
    }

    private void ClearSelection()
    {
        _selectedFile = null;
        FileNameLabel.Text = "Nincs kiválasztott fájl";
        FileInfoLabel.Text = "Csak .xlsx formátum";
        UploadButton.IsEnabled = false;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorCard.IsVisible = true;
        ResultCard.IsVisible = false;
    }

    private void HideMessages()
    {
        ErrorCard.IsVisible = false;
        ResultCard.IsVisible = false;
    }
}