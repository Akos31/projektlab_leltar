using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InventoryApp.Hubs
{
    // Valós idejű értesítések, csak a szerver küld (IHubContext-en keresztül)
    [Authorize]
    public class InventoryHub : Hub
    {
        public const string Path = "/hubs/inventory";

        public const string ScanCreated = "ScanCreated";
        public const string PeriodChanged = "PeriodChanged";
    }
}