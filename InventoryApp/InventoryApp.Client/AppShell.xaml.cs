using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    // Bejelentkezés után a szerepkör alapján elrejti a nem engedélyezett füleket.
    // (Csak kényelmi funkció - a valódi védelem a szerveren van.)
    public void ApplyPermissions(UserSession session)
    {
        ScanTab.IsVisible = session.CanScan;
        ImportTab.IsVisible = session.CanImport;
        // Később: ReportsTab.IsVisible = session.CanViewReports;
    }
}