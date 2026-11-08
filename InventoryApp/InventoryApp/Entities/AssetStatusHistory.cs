using System;

namespace InventoryApp.Entities
{
    public class AssetStatusHistory
    {
        public int Id { get; set; }

        public int AssetId { get; set; }
        public Asset? Asset { get; set; }

        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public int? ChangedByUserId { get; set; }
        public User? ChangedByUser { get; set; }
    }
}