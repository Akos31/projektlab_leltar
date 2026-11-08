using InventoryApp.Data;
using InventoryApp.Dtos;
using InventoryApp.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    // Kiértékelés: lezárt leltári időszakok összehasonlítása.
    // Adminisztrátor és kiértékelő számára.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Kiertekelo)]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReportsController(AppDbContext db) => _db = db;

        // GET /api/reports/closed-periods?zoneId=3
        // Lezárt időszakok (legújabb elöl), opcionálisan egy körzetre szűrve
        [HttpGet("closed-periods")]
        public async Task<IActionResult> GetClosedPeriods([FromQuery] int? zoneId = null)
        {
            var list = await _db.InventoryPeriods.AsNoTracking()
                .Where(p => p.Status == "closed")
                .Where(p => zoneId == null || p.InventoryZoneId == zoneId)
                .OrderBy(p => p.InventoryZone!.Code)
                .ThenByDescending(p => p.StartDate)
                .Select(p => new ClosedPeriodDto(
                    p.Id,
                    p.InventoryZoneId,
                    p.InventoryZone!.Code,
                    p.InventoryZone.Name,
                    p.StartDate,
                    p.EndDate,
                    p.Scans.Count))
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/reports/compare?periodA=4&periodB=9
        // Két lezárt időszak összehasonlítása eszközönként. A sorrend mindegy:
        // "A" mindig a korábbi, "B" a későbbi időszak lesz.
        [HttpGet("compare")]
        public async Task<IActionResult> Compare([FromQuery] int periodA, [FromQuery] int periodB)
        {
            if (periodA == periodB)
                return BadRequest(new { message = "Két különböző időszakot válassz." });

            var periods = await _db.InventoryPeriods.AsNoTracking()
                .Include(p => p.InventoryZone)
                .Where(p => p.Id == periodA || p.Id == periodB)
                .ToListAsync();

            if (periods.Count != 2)
                return NotFound(new { message = "Nem található mindkét időszak." });
            if (periods.Any(p => p.Status != "closed"))
                return BadRequest(new { message = "Csak lezárt időszakok hasonlíthatók össze." });
            if (periods[0].InventoryZoneId != periods[1].InventoryZoneId)
                return BadRequest(new { message = "Csak ugyanannak a körzetnek az időszakai hasonlíthatók össze." });

            var ordered = periods.OrderBy(p => p.StartDate).ThenBy(p => p.Id).ToList();
            var a = ordered[0];
            var b = ordered[1];
            var zone = a.InventoryZone!;

            // A két időszak összes beolvasása (helyiségszámmal)
            var scans = await _db.Scans.AsNoTracking()
                .Where(s => s.InventoryPeriodId == a.Id || s.InventoryPeriodId == b.Id)
                .Select(s => new
                {
                    s.AssetId,
                    s.InventoryPeriodId,
                    s.ScannedQuantity,
                    s.IsOtherZone,
                    Room = s.Room!.RoomNumber
                })
                .ToListAsync();

            var scannedIds = scans.Select(s => s.AssetId).Distinct().ToList();

            // A körzet saját eszközei + amiket máshonnan olvastak be ide
            var assets = await _db.Assets.AsNoTracking()
                .Where(x => x.InventoryZoneId == zone.Id || scannedIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.SourceId,
                    x.Name,
                    x.Status,
                    x.ExpectedQuantity,
                    x.InventoryZoneId,
                    Type = x.AssetType!.Name,
                    InventoryNumber = x.Codes
                        .Where(c => c.IsPrimary)
                        .Select(c => c.CodeValue)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var byAssetAndPeriod = scans
                .GroupBy(s => (s.AssetId, s.InventoryPeriodId))
                .ToDictionary(
                    g => g.Key,
                    g => new ComparePeriodState(
                        g.Sum(s => s.ScannedQuantity),
                        g.Select(s => s.Room).Distinct().OrderBy(r => r).ToList(),
                        g.Any(s => s.IsOtherZone)));

            var empty = new ComparePeriodState(0, new List<string>(), false);

            var items = assets
                .Select(x =>
                {
                    var stateA = byAssetAndPeriod.GetValueOrDefault((x.Id, a.Id)) ?? empty;
                    var stateB = byAssetAndPeriod.GetValueOrDefault((x.Id, b.Id)) ?? empty;
                    return new CompareItemDto(
                        x.Id, x.SourceId, x.Name, x.InventoryNumber, x.Type, x.Status,
                        x.ExpectedQuantity, x.InventoryZoneId == zone.Id,
                        stateA, stateB, Classify(stateA, stateB));
                })
                // a legfontosabb változások elöl
                .OrderBy(i => ChangeOrder(i.ChangeType))
                .ThenBy(i => i.Name)
                .ThenBy(i => i.SourceId)
                .ToList();

            var summary = new CompareSummaryDto(
                items.Count,
                items.Count(i => i.ChangeType == "unchanged"),
                items.Count(i => i.ChangeType == "moved"),
                items.Count(i => i.ChangeType == "quantity"),
                items.Count(i => i.ChangeType == "missing"),
                items.Count(i => i.ChangeType == "found"),
                items.Count(i => i.ChangeType == "notfound"));

            return Ok(new CompareResultDto(
                zone.Id, zone.Code, zone.Name,
                ToPeriodDto(a, scans.Where(s => s.InventoryPeriodId == a.Id)
                    .Select(s => (s.AssetId, s.ScannedQuantity))),
                ToPeriodDto(b, scans.Where(s => s.InventoryPeriodId == b.Id)
                    .Select(s => (s.AssetId, s.ScannedQuantity))),
                summary,
                items));
        }

        // --- Segédmetódusok ---

        private static string Classify(ComparePeriodState a, ComparePeriodState b)
        {
            if (a.Quantity == 0 && b.Quantity == 0) return "notfound";
            if (b.Quantity == 0) return "missing";
            if (a.Quantity == 0) return "found";
            if (a.Quantity != b.Quantity) return "quantity";
            return a.Rooms.SequenceEqual(b.Rooms) ? "unchanged" : "moved";
        }

        private static int ChangeOrder(string changeType) => changeType switch
        {
            "missing" => 0,
            "found" => 1,
            "quantity" => 2,
            "moved" => 3,
            "notfound" => 4,
            _ => 5
        };

        private static ComparePeriodDto ToPeriodDto(
            InventoryPeriod p, IEnumerable<(int AssetId, int Quantity)> scans)
        {
            var list = scans.ToList();
            return new ComparePeriodDto(
                p.Id, p.StartDate, p.EndDate,
                list.Count,
                list.Select(s => s.AssetId).Distinct().Count(),
                list.Sum(s => s.Quantity));
        }
    }
}
