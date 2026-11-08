namespace InventoryApp.Dtos
{
    // Egy lezárt leltári időszak a választólistához
    public record ClosedPeriodDto(
        int Id,
        int ZoneId,
        string ZoneCode,
        string ZoneName,
        DateTime StartDate,
        DateTime? EndDate,
        int ScanCount);

    // Egy időszak összesítője az összehasonlításban
    public record ComparePeriodDto(
        int Id,
        DateTime StartDate,
        DateTime? EndDate,
        int ScanCount,
        int AssetCount,     // hány különböző eszközt olvastak be
        int TotalQuantity); // összesen hány darabot

    // Egy eszköz állapota egy időszakban
    public record ComparePeriodState(
        int Quantity,
        List<string> Rooms,
        bool OtherZone);

    // Változás kategóriák (ChangeType):
    //   unchanged - mindkettőben megvan, ugyanannyi, ugyanott
    //   moved     - mindkettőben ugyanannyi, de más helyiség(ek)ben
    //   quantity  - mindkettőben megvan, de eltér a darabszám
    //   missing   - a korábbiban megvolt, a későbbiben nem került elő
    //   found     - a korábbiban nem volt meg, a későbbiben előkerült
    //   notfound  - egyikben sem olvasták be (a körzet eszköze)
    public record CompareItemDto(
        int AssetId,
        string SourceId,
        string Name,
        string? InventoryNumber,
        string? Type,
        string Status,
        int ExpectedQuantity,
        bool BelongsToZone,
        ComparePeriodState A,
        ComparePeriodState B,
        string ChangeType);

    public record CompareSummaryDto(
        int Total,
        int Unchanged,
        int Moved,
        int Quantity,
        int Missing,
        int Found,
        int NotFound);

    public record CompareResultDto(
        int ZoneId,
        string ZoneCode,
        string ZoneName,
        ComparePeriodDto PeriodA,
        ComparePeriodDto PeriodB,
        CompareSummaryDto Summary,
        List<CompareItemDto> Items);
}
