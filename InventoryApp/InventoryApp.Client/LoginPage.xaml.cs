using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;
    private readonly UserSession _session;

    // Az ideiglenes jelszóval való belépés adatai a kötelező jelszócseréhez
    private string? _pendingUsername;
    private string? _pendingTempPassword;

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
            var login = await _api.LoginAsync(username, password);
            PasswordEntry.Text = string.Empty;

            if (login.MustChangePassword)
            {
                ShowChangePassword(login.Username, password, login.DisplayName);
                return;
            }

            await EnterAppAsync();
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

    private void ShowChangePassword(string username, string tempPassword, string displayName)
    {
        _pendingUsername = username;
        _pendingTempPassword = tempPassword;

        ChangeInfoLabel.Text =
            $"Szia {displayName}! Ideiglenes jelszóval léptél be, ezért a folytatáshoz " +
            "meg kell adnod egy saját jelszót. Az ideiglenes jelszó ezután megszűnik.";
        NewPasswordEntry.Text = string.Empty;
        ConfirmPasswordEntry.Text = string.Empty;

        LoginPanel.IsVisible = false;
        ChangePasswordPanel.IsVisible = true;
        NewPasswordEntry.Focus();
    }

    private async void OnSetPasswordClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (_pendingUsername is null || _pendingTempPassword is null) return;

        var newPassword = NewPasswordEntry.Text ?? string.Empty;
        if (newPassword != (ConfirmPasswordEntry.Text ?? string.Empty))
        {
            ShowError("A két jelszó nem egyezik.");
            return;
        }

        SetPasswordButton.IsEnabled = false;
        try
        {
            var (ok, error) = await _api.ChangePasswordAsync(_pendingUsername, _pendingTempPassword, newPassword);
            if (!ok)
            {
                ShowError(error ?? "Nem sikerült a jelszócsere.");
                return;
            }

            ResetChangePassword();
            await EnterAppAsync();
        }
        catch (Exception ex)
        {
            ShowError($"A szerver nem érhető el: {ex.Message}");
        }
        finally
        {
            SetPasswordButton.IsEnabled = true;
        }
    }

    private void OnCancelChangeClicked(object sender, EventArgs e)
    {
        ResetChangePassword();
        ErrorLabel.IsVisible = false;
    }

    private void ResetChangePassword()
    {
        _pendingUsername = null;
        _pendingTempPassword = null;
        NewPasswordEntry.Text = string.Empty;
        ConfirmPasswordEntry.Text = string.Empty;
        ChangePasswordPanel.IsVisible = false;
        LoginPanel.IsVisible = true;
    }

    private async Task EnterAppAsync()
    {
        if (Shell.Current is AppShell shell)
            shell.ApplyPermissions(_session);

        // Aki szkennelhet, a Beolvasásra kerül, a kiértékelő az Eszközökre
        await Shell.Current.GoToAsync(_session.CanScan ? "//ScanPage" : "//AssetsPage");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}