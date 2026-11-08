namespace InventoryApp.Client.Models;

public class ResponsiblePersonItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AssetTypeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AssetCount { get; set; }

    public string Display => AssetCount > 0 ? $"{Name} ({AssetCount} db)" : Name;
}