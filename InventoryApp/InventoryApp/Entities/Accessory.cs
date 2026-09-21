namespace InventoryApp.Entities
{
    // Attachments/components belonging to a main asset (source Excel: rows with Alszám > 0)
    public class Accessory
    {
        public int Id { get; set; }

        public int AssetId { get; set; }
        public Asset? Asset { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        // Alszam
        public int SubNumber { get; set; }

        // Valaki altal le lett ellenorizve
        public bool VerifiedDirectly { get; set; } = false;
    }
}
