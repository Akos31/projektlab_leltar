using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;
    private readonly UserSession _session;

    public LoginPage(ApiService api, UserSession session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Add meg a felhasználóneved és a jelszavad.");
            return;
        }

        LoginButton.IsEnabled = false;
        try
        {
            await _api.LoginAsync(username, password);
            PasswordEntry.Text = string.Empty;

            if (Shell.Current is AppShell shell)
                shell.ApplyPermissions(_session);

            // Aki szkennelhet, a Beolvasásra kerül, a kiértékelő az Eszközökre
            await Shell.Current.GoToAsync(_session.CanScan ? "//ScanPage" : "//AssetsPage");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}