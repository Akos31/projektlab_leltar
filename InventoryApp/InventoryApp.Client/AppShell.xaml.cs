using InventoryApp.Client.Services;

namespace InventoryApp.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    public void ApplyPermissions(UserSession session)
    {
        ScanTab.IsVisible = session.CanScan;
        ImportTab.IsVisible = session.CanImport;
        ZonesTab.IsVisible = session.CanManageZones;
        // Később: ReportsTab.IsVisible = session.CanViewReports;
    }
}