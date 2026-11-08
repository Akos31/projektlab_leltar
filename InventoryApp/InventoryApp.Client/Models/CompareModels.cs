namespace InventoryApp.Client.Models;

// GET api/reports/closed-periods egy eleme
public class ClosedPeriodItem
{
    public int Id { get; set; }
    public int ZoneId { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int ScanCount { get; set; }

    public string ZoneDisplay =>
        string.IsNullOrWhiteSpace(ZoneName) || ZoneName == ZoneCode ? ZoneCode : $"{ZoneCode} – {ZoneName}";

    // A Picker a ToString()-et jeleníti meg
    public override string ToString() =>
        $"#{Id} · {StartDate.ToLocalTime():yyyy.MM.dd} – " +
        $"{EndDate?.ToLocalTime().ToString("yyyy.MM.dd") ?? "?"} ({ScanCount} beolvasás)";
}

// Körzet a választóhoz, a lezárt időszakaival
public class CompareZoneOption
{
    public int ZoneId { get; set; }
    public string Display { get; set; } = string.Empty;
    public List<ClosedPeriodItem> Periods { get; set; } = new();

    public override string ToString() => $"{Display} ({Periods.Count} lezárt)";
}

public class ComparePeriod
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int ScanCount { get; set; }
    public int AssetCount { get; set; }
    public int TotalQuantity { get; set; }

    public string Range =>
        $"{StartDate.ToLocalTime():yyyy.MM.dd} – {EndDate?.ToLocalTime().ToString("yyyy.MM.dd") ?? "?"}";
    public string Detail => $"{AssetCount} eszköz · {TotalQuantity} db · {ScanCount} beolvasás";
}

public class ComparePeriodState
{
    public int Quantity { get; set; }
    public List<string> Rooms { get; set; } = new();
    public bool OtherZone { get; set; }

    public string Text => Quantity == 0
        ? "nincs meg"
        : $"{Quantity} db · {(Rooms.Count == 0 ? "?" : string.Join(", ", Rooms))}";
}

public class CompareItem
{
    public int AssetId { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? InventoryNumber { get; set; }
    public string? Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public bool BelongsToZone { get; set; }
    public ComparePeriodState A { get; set; } = new();
    public ComparePeriodState B { get; set; } = new();
    public string ChangeType { get; set; } = string.Empty;

    // --- Megjelenítéshez ---

    public string Title => string.IsNullOrWhiteSpace(InventoryNumber) ? Name : $"{InventoryNumber} – {Name}";
    public string ChangeText => CompareCategories.Label(ChangeType);
    public Color ChangeColor => CompareCategories.ColorOf(ChangeType);
    public string AText => $"Korábbi: {A.Text}";
    public string BText => $"Későbbi: {B.Text}";

    public string Meta
    {
        get
        {
            var parts = new List<string> { $"Elvárt: {ExpectedQuantity} db" };
            if (Status != "active") parts.Add($"Állapot: {AssetSummary.ToStatusText(Status)}");
            if (!BelongsToZone) parts.Add("⚠ más körzet eszköze");
            return string.Join(" · ", parts);
        }
    }

    public bool Matches(string search) =>
        Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        SourceId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        (InventoryNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
        A.Rooms.Concat(B.Rooms).Any(r => r.Contains(search, StringComparison.OrdinalIgnoreCase));
}

public class CompareSummary
{
    public int Total { get; set; }
    public int Unchanged { get; set; }
    public int Moved { get; set; }
    public int Quantity { get; set; }
    public int Missing { get; set; }
    public int Found { get; set; }
    public int NotFound { get; set; }
}

public class CompareResult
{
    public int ZoneId { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public ComparePeriod PeriodA { get; set; } = new();
    public ComparePeriod PeriodB { get; set; } = new();
    public CompareSummary Summary { get; set; } = new();
    public List<CompareItem> Items { get; set; } = new();
}

// Összesítő csempe (egyben szűrő is)
public class CompareTile
{
    public string Key { get; set; } = string.Empty;   // kategória kód, "changes" vagy "all"
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public Color Accent { get; set; } = Colors.Gray;
    public bool IsSelected { get; set; }

    public double Opacity => IsSelected ? 1.0 : 0.55;
    public double BorderWidth => IsSelected ? 2 : 1;
}

public static class CompareCategories
{
    public const string All = "all";
    public const string Changes = "changes";

    public static readonly string[] ChangeTypes = { "missing", "found", "quantity", "moved" };

    public static string Label(string type) => type switch
    {
        All => "Összes",
        Changes => "Eltérések",
        "missing" => "Eltűnt",
        "found" => "Előkerült",
        "quantity" => "Darabszám változott",
        "moved" => "Áthelyezve",
        "notfound" => "Egyikben sincs meg",
        "unchanged" => "Változatlan",
        _ => type
    };

    public static Color ColorOf(string type) => Color.FromArgb(type switch
    {
        "missing" => "#C62828",
        "found" => "#6A1B9A",
        "quantity" => "#EF6C00",
        "moved" => "#1565C0",
        "notfound" => "#616161",
        "unchanged" => "#2E7D32",
        Changes => "#B45309",
        _ => "#374151"
    });

    public static bool Matches(string filter, string changeType) => filter switch
    {
        All => true,
        Changes => ChangeTypes.Contains(changeType),
        _ => filter == changeType
    };
}
