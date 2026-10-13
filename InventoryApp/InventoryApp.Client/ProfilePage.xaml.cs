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

        PermissionsLabel.Text = "Jogosultságok:\n" + string.Join("\n", permissions);
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