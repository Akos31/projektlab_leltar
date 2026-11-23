namespace InventoryApp.Dtos
{
    public record ScanListItemDto(
        int Id,
        DateTime Timestamp,
        int ScannedQuantity,
        bool IsOtherZone,
        int? UserId,
        int InventoryPeriodId,
        int AssetId,
        string AssetName,
        string? InventoryNumber,
        int ZoneId,
        string Zone,
        int RoomId,
        string Room);

    public record PeriodChangedDto(int ZoneId, int? OpenPeriodId);
}