using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class ProfilePage : ContentPage
{
    private readonly ApiService _api;
    private readonly UserSession _session;

    public ProfilePage(ApiService api, UserSession session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        DisplayNameLabel.Text = _session.Current?.DisplayName;
        UsernameLabel.Text = $"@{_session.Current?.Username}";
        RoleLabel.Text = _session.RoleDisplayName;

        var permissions = new List<string> { "• Eszközök megtekintése" };
        if (_session.CanScan) permissions.Add("• Leltározás (beolvasás)");
        if (_session.CanImport) permissions.Add("• Excel import");
        if (_session.CanViewReports) permissions.Add("• Jelentések és export");
        if (_session.CanManageUsers) permissions.Add("• Felhasználók kezelése");

        PermissionsLabel.Text = "Jogosultságok:\n" + string.Join("\n", permissions);

        PasswordMessageLabel.IsVisible = false;
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        var username = _session.Current?.Username;
        if (username is null) return;

        var current = CurrentPasswordEntry.Text ?? string.Empty;
        var newPassword = NewPasswordEntry.Text ?? string.Empty;

        if (current.Length == 0)
        {
            ShowPasswordMessage("Add meg a jelenlegi jelszavad.", error: true);
            return;
        }
        if (newPassword != (ConfirmPasswordEntry.Text ?? string.Empty))
        {
            ShowPasswordMessage("A két új jelszó nem egyezik.", error: true);
            return;
        }

        ChangePasswordButton.IsEnabled = false;
        try
        {
            // Siker esetén az ApiService a szervertől kapott új tokent használja tovább
            var (ok, error) = await _api.ChangePasswordAsync(username, current, newPassword);
            if (!ok)
            {
                ShowPasswordMessage(error ?? "Nem sikerült a jelszócsere.", error: true);
                return;
            }

            CurrentPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
            ShowPasswordMessage("A jelszavad megváltozott.", error: false);
        }
        catch (Exception ex)
        {
            ShowPasswordMessage($"A szerver nem érhető el: {ex.Message}", error: true);
        }
        finally
        {
            ChangePasswordButton.IsEnabled = true;
        }
    }

    private void ShowPasswordMessage(string text, bool error)
    {
        PasswordMessageLabel.Text = text;
        PasswordMessageLabel.TextColor = error ? Color.FromArgb("#C62828") : Color.FromArgb("#2E7D32");
        PasswordMessageLabel.IsVisible = true;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Kijelentkezés", "Biztosan kijelentkezel?", "Igen", "Mégse");
        if (!confirm)
            return;

        _api.Logout();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}