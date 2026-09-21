namespace InventoryApp.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ScanPage), typeof(ScanPage));
    }
}