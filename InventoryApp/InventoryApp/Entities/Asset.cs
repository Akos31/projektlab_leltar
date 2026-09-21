using System;
using System.Collections.Generic;

namespace InventoryApp.Entities
{
    // Core inventory item (source/master data - "leltári forrásadat")
    public class Asset
    {
        public int Id { get; set; }

        // The original "Eszköz" number from the source Excel, kept for traceability
        public string SourceId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int ExpectedQuantity { get; set; }

        // active / decommissioned / lost / stolen / other
        public string Status { get; set; } = "active";

        public DateTime StatusChangedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ActivationDate { get; set; }

        // Financial fields - kept for reference, not used in inventory logic
        public decimal? PurchaseValue { get; set; }
        public decimal? NetBookValue { get; set; }

        public int AssetTypeId { get; set; }
        public AssetType? AssetType { get; set; }

        public int InventoryZoneId { get; set; }
        public InventoryZone? InventoryZone { get; set; }

        public int? ResponsiblePersonId { get; set; }
        public ResponsiblePerson? ResponsiblePerson { get; set; }

        public List<AssetCode> Codes { get; set; } = new();
        public List<Accessory> Accessories { get; set; } = new();
        public List<Scan> Scans { get; set; } = new();
    }
}
