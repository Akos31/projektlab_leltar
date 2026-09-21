namespace InventoryApp.Client.Models;

public class AssetSummary
{
    public int Id { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Zone { get; set; }
    public int AccessoryCount { get; set; }
}