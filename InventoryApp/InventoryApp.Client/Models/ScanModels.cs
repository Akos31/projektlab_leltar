namespace InventoryApp.Client.Models
{
    public class ZoneItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int? OpenPeriodId { get; set; }
        public string Display => string.IsNullOrWhiteSpace(Name) ? Code : $"{Code} – {Name}";
    }

    public class RoomItem
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = "";
    }

    public class PeriodItem
    {
        public int Id { get; set; }
    }

    public class ScanAssetRef
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ScanResponse
    {
        public string? Result { get; set; }       // Ok, OtherZone, UnknownCode, NoOpenPeriod, ...
        public string? Message { get; set; }
        public ScanAssetRef? Asset { get; set; }
        public int ExpectedQuantity { get; set; }
        public int ScannedTotal { get; set; }
    }
    public class ScanItem
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int ScannedQuantity { get; set; }
        public bool IsOtherZone { get; set; }
        public int? UserId { get; set; }
        public int InventoryPeriodId { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; } = "";
        public string? InventoryNumber { get; set; }
        public int ZoneId { get; set; }
        public string Zone { get; set; } = "";
        public int RoomId { get; set; }
        public string Room { get; set; } = "";

        public string Title => $"{InventoryNumber} – {AssetName}";
        public string Detail =>
            $"{Zone} / {Room} · {ScannedQuantity} db · {Timestamp.ToLocalTime():yyyy.MM.dd HH:mm:ss}";
    }
}