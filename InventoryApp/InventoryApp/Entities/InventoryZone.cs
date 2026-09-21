using System.Collections.Generic;

namespace InventoryApp.Entities
{
    // Administrative/organizational grouping of assets, derived from the
    // source file's "Telephely" column (e.g. "261", "262").
    public class InventoryZone
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public List<Asset> Assets { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
        public List<InventoryPeriod> InventoryPeriods { get; set; } = new();
    }
}
