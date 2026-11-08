using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryApp.Client.Models;

// PUT api/assets/{id}/status válasza
public class AssetStatusResult
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StatusChangedAt { get; set; }
}

// GET api/assets/{id}/status-history egy eleme
public class AssetStatusHistoryEntry
{
    public int Id { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangedByName { get; set; }

    public string ChangeText => $"{AssetSummary.ToStatusText(OldStatus)} → {AssetSummary.ToStatusText(NewStatus)}";
    public string ChangedAtText => ChangedAt.ToLocalTime().ToString("yyyy.MM.dd. HH:mm");
    public string ChangedByText => string.IsNullOrWhiteSpace(ChangedByName) ? "Ismeretlen felhasználó" : ChangedByName;
}
