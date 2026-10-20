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
    public int? OpenPeriodId { get; set; }
    public int ScannedTotal { get; set; }
    public bool HasWrongZoneScan { get; set; }

    // --- Megjelenítéshez számolt mezők (a szerver nem küldi) ---

    public string DisplayType => ToDisplayType(Type);

    public string StatusText => ToStatusText(Status);

    public string ScanState => OpenPeriodId is null
    ? "imported"
    : ScannedTotal <= 0
        ? "none"
        : ScannedTotal < ExpectedQuantity
            ? "partial"
            : ScannedTotal == ExpectedQuantity
                ? "exact"
                : "surplus";

    public string ScanStatusText => ScanState switch
    {
        "imported" => "Beimportálva",
        "none" => "Nem beolvasott",
        "partial" => $"Részben beolvasott ({ScannedTotal}/{ExpectedQuantity})",
        "exact" => $"Pontosan megvan ({ScannedTotal}/{ExpectedQuantity})",
        "surplus" => $"Túl van olvasva ({ScannedTotal}/{ExpectedQuantity})",
        _ => ScanState
    };

    public bool ShowZoneCorrectness => ScannedTotal > 0;

    public string ZoneCorrectnessText => HasWrongZoneScan ? "⚠ Rossz körzetben" : "✓ Jó körzetben";

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