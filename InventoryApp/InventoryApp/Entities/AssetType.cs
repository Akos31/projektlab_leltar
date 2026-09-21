using System.Collections.Generic;

namespace InventoryApp.Entities
{
    // Extensible category/type (e.g. "SZÉK", "SZÁMÍTÓGÉP"). Not hardcoded -
    // new types are created on the fly during import, and can be merged later
    // by an administrator if the source data contains near-duplicate names.
    public class AssetType
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Asset> Assets { get; set; } = new();
    }
}
