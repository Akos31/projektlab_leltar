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

    public string DisplayType => ToDisplayType(Type);

    public string StatusText => ToStatusText(Status);

    public string ZoneText => string.IsNullOrWhiteSpace(Zone) ? "Nincs zóna" : $"Zóna {Zone}";

    public bool HasAccessories => AccessoryCount > 0;

    public string AccessoryText => $"{AccessoryCount} tartozék";

    // --- Segédfüggvények (a szűrőkhöz is kellenek) ---

    // "VONALKÓD LEOLVASÓ" -> "Vonalkód leolvasó"
    public static string ToDisplayType(string? type) => string.IsNullOrWhiteSpace(type)
        ? "Nincs típus"
        : char.ToUpperInvariant(type[0]) + type[1..].ToLowerInvariant();

    public static string ToStatusText(string? status) => status switch
    {
        "active" => "Aktív",
        "decommissioned" => "Kivonva",
        "lost" => "Elveszett",
        "stolen" => "Ellopva",
        "other" => "Egyéb",
        _ => status ?? string.Empty
    };
}