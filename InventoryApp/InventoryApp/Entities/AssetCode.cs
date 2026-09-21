namespace InventoryApp.Entities
{
    // Multiple codes can identify the same asset (Leltárszám, Eredeti eszköz, Adóhivatal, ...)
    public class AssetCode
    {
        public int Id { get; set; }

        public int AssetId { get; set; }
        public Asset? Asset { get; set; }

        public string CodeValue { get; set; } = string.Empty;

        // e.g. "InventoryNumber", "OriginalAssetNumber", "TaxReference"
        public string CodeType { get; set; } = string.Empty;

        // The code the scanner app should actually match against during scanning.
        // Decision: "Leltárszám" (InventoryNumber) is primary.
        public bool IsPrimary { get; set; }
    }
}
