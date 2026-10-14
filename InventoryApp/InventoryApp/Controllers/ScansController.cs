using InventoryApp.Data;
using InventoryApp.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryApp.Controllers
{
    public class ScanRequest
    {
        public string Code { get; set; } = string.Empty;
        public int InventoryZoneId { get; set; }   // aktív körzet
        public int RoomId { get; set; }            // kiválasztott helyiség
        public int? UserId { get; set; }           // később a bejelentkezésből jön
        public int Quantity { get; set; } = 1;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScansController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ScansController(AppDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Create(ScanRequest req)
        {
            var code = req.Code?.Trim() ?? "";
            if (code.Length == 0)
                return BadRequest(new { result = "InvalidInput", message = "Üres kód." });
            if (req.Quantity < 1)
                return BadRequest(new { result = "InvalidInput", message = "A darabszám legalább 1." });

            var period = await _db.InventoryPeriods
                .FirstOrDefaultAsync(p => p.InventoryZoneId == req.InventoryZoneId && p.Status == "open");
            if (period == null)
                return BadRequest(new { result = "NoOpenPeriod", message = "A körzetben nincs nyitott leltári időszak." });

            // a helyiségnek az aktív körzethez kell tartoznia
            var roomOk = await _db.Rooms.AnyAsync(r =>
                r.Id == req.RoomId && r.InventoryZoneId == req.InventoryZoneId);
            if (!roomOk)
                return BadRequest(new { result = "InvalidRoom", message = "Érvénytelen helyiség ebben a körzetben." });

            // azonosítás bármely kódtípus alapján
            var asset = await _db.Assets
                .Where(a => a.Codes.Any(c => c.CodeValue == code))
                .FirstOrDefaultAsync();
            if (asset == null)
                return NotFound(new { result = "UnknownCode", message = $"Ismeretlen kód: {code}" });

            var scan = new Scan
            {
                AssetId = asset.Id,
                InventoryPeriodId = period.Id,
                InventoryZoneId = req.InventoryZoneId,
                RoomId = req.RoomId,
                UserId = req.UserId,
                Timestamp = DateTime.UtcNow,
                ScannedQuantity = req.Quantity,
                IsOtherZone = asset.InventoryZoneId != req.InventoryZoneId
            };
            _db.Scans.Add(scan);
            await _db.SaveChangesAsync();

            var scannedTotal = await _db.Scans
                .Where(s => s.AssetId == asset.Id && s.InventoryPeriodId == period.Id)
                .SumAsync(s => s.ScannedQuantity);

            return Ok(new
            {
                result = scan.IsOtherZone ? "OtherZone" : "Ok",
                scanId = scan.Id,
                asset = new { asset.Id, asset.Name },
                expectedQuantity = asset.ExpectedQuantity,
                scannedTotal
            });
        }
        // Az összes beolvasás, opcionális szűrőkkel (legújabb elöl)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? zoneId,
            [FromQuery] int? periodId,
            [FromQuery] int? roomId,
            [FromQuery] int? assetId,
            [FromQuery] bool? otherZone)
        {
            var q = _db.Scans.AsQueryable();

            if (zoneId != null) q = q.Where(s => s.InventoryZoneId == zoneId);
            if (periodId != null) q = q.Where(s => s.InventoryPeriodId == periodId);
            if (roomId != null) q = q.Where(s => s.RoomId == roomId);
            if (assetId != null) q = q.Where(s => s.AssetId == assetId);
            if (otherZone != null) q = q.Where(s => s.IsOtherZone == otherZone);

            var list = await q
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new
                {
                    s.Id,
                    s.Timestamp,
                    s.ScannedQuantity,
                    s.IsOtherZone,
                    s.UserId,
                    s.InventoryPeriodId,
                    AssetId = s.AssetId,
                    AssetName = s.Asset!.Name,
                    InventoryNumber = s.Asset.Codes
                        .Where(c => c.IsPrimary)
                        .Select(c => c.CodeValue)
                        .FirstOrDefault(),
                    ZoneId = s.InventoryZoneId,
                    Zone = s.InventoryZone!.Code,
                    RoomId = s.RoomId,
                    Room = s.Room!.RoomNumber
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}