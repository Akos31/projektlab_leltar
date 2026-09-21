namespace InventoryApp.Client;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Egyelőre nincs valódi hitelesítés (JWT) - ez csak átnavigál,
        // hogy a kliens-szerver kapcsolatot bizonyítsuk a ScanPage-en.
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text))
        {
            ErrorLabel.Text = "Add meg a felhasználóneved.";
            ErrorLabel.IsVisible = true;
            return;
        }

        await Shell.Current.GoToAsync(nameof(ScanPage));
    }
}