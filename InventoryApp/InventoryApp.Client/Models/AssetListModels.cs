namespace InventoryApp.Client.Models;

// A lapozott válasz: { items: [...], totalCount: 1791 }
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

// GET api/assets/filters válasza
public class AssetFilterOptions
{
    public List<string> Statuses { get; set; } = new();
    public List<string> Types { get; set; } = new();
    public List<string> Zones { get; set; } = new();
}

// Az aktuális keresési/szűrési feltételek
public class AssetQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Zone { get; set; }
}

// Egy legördülő elem: a szervernek a Value megy, a felhasználó a Label-t látja.
// (A Picker a ToString()-et jeleníti meg.)
public record FilterOption(string? Value, string Label)
{
    public override string ToString() => Label;
}