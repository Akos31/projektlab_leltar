using System;
using System.Collections.Generic;

namespace InventoryApp.Entities
{
    // A single stocktaking period for one inventory zone. Scans belong to a
    // period, so results from different periods stay separate and comparable.
    public class InventoryPeriod
    {
        public int Id { get; set; }

        public int InventoryZoneId { get; set; }
        public InventoryZone? InventoryZone { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // open / closed - a zone can only have one open period at a time
        public string Status { get; set; } = "open";

        public List<Scan> Scans { get; set; } = new();
    }
}
