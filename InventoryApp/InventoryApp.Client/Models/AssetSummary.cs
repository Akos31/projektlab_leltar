namespace InventoryApp.Client.Models;

public class AssetSummary
{
    public int Id { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? InventoryNumber { get; set; }
    public string? Type { get; set; }
    public string? Zone { get; set; }
    public int AccessoryCount { get; set; }

    // --- Megjelenítéshez számolt mezők (a szerver nem küldi) ---

    // "VONALKÓD LEOLVASÓ" -> "Vonalkód leolvasó"
    public string DisplayType => string.IsNullOrWhiteSpace(Type)
        ? "Nincs típus"
        : char.ToUpperInvariant(Type[0]) + Type[1..].ToLowerInvariant();

    public string StatusText => Status switch
    {
        "active" => "Aktív",
        "decommissioned" => "Kivonva",
        "lost" => "Elveszett",
        "stolen" => "Ellopva",
        "other" => "Egyéb",
        _ => Status
    };

    public string ZoneText => string.IsNullOrWhiteSpace(Zone) ? "Nincs zóna" : $"Zóna {Zone}";

    public bool HasAccessories => AccessoryCount > 0;

    public string AccessoryText => $"{AccessoryCount} tartozék";
}