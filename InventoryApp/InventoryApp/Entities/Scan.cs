using System;

namespace InventoryApp.Entities
{
    // One individual scan event during a stocktake. Kept as its own
    // independent, timestamped record so that source data never gets
    // overwritten and every period's results stay reconstructable later.
    public class Scan
    {
        public int Id { get; set; }

        public int AssetId { get; set; }
        public Asset? Asset { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }

        public int InventoryPeriodId { get; set; }
        public InventoryPeriod? InventoryPeriod { get; set; }

        // The zone the scan was actually recorded under - may differ from
        // the asset's own InventoryZoneId if it turned up in the wrong zone.
        public int InventoryZoneId { get; set; }
        public InventoryZone? InventoryZone { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int ScannedQuantity { get; set; }
    }
}
