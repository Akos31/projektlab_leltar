namespace InventoryApp.Client.Models;

// A lapozott válasz: { items: [...], totalCount: 1791 }
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
public class AssetFilterOptions
{
    public List<string> Statuses { get; set; } = new();
    public List<string> Types { get; set; } = new();
    public List<string> Zones { get; set; } = new();
    public List<IdLabelOption> Rooms { get; set; } = new();
    public List<IdLabelOption> ResponsiblePersons { get; set; } = new();
}

public class IdLabelOption
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class AssetQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Zone { get; set; }
    public int? RoomId { get; set; }
    public int? ResponsiblePersonId { get; set; }
}

public record FilterOption(string? Value, string Label)
{
    public override string ToString() => Label;
}