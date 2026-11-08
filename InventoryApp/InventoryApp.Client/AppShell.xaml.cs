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
        AssetTypesTab.IsVisible = session.CanManageAssetTypes;
        UsersTab.IsVisible = session.CanManageUsers;
        // Később: ReportsTab.IsVisible = session.CanViewReports;
    }
}