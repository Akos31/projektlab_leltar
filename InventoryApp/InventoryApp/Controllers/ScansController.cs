using InventoryApp.Data;
using InventoryApp.Dtos;
using InventoryApp.Entities;
using InventoryApp.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using System.Security.Claims;

namespace InventoryApp.Controllers
{
    public class ScanRequest
    {
        public string Code { get; set; } = string.Empty;
        public int InventoryZoneId { get; set; }
        public int RoomId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScansController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<InventoryHub> _hub;

        public ScansController(AppDbContext db, IHubContext<InventoryHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // közös vetítés a listához és az élő értesítéshez
        private static readonly Expression<Func<Scan, ScanListItemDto>> ToListItem = s =>
            new ScanListItemDto(
                s.Id,
                s.Timestamp,
                s.ScannedQuantity,
                s.IsOtherZone,
                s.UserId,
                s.InventoryPeriodId,
                s.AssetId,
                s.Asset!.Name,
                s.Asset.Codes.Where(c => c.IsPrimary).Select(c => c.CodeValue).FirstOrDefault(),
                s.InventoryZoneId,
                s.InventoryZone!.Code,
                s.RoomId,
                s.Room!.RoomNumber);

        [HttpPost]
        [Authorize(Roles = Roles.Admin + "," + Roles.Leltarozo)]
        public async Task<IActionResult> Create(ScanRequest req)
        {
            var code = req.Code?.Trim() ?? "";
            if (code.Length == 0)
                return BadRequest(new { result = "InvalidInput", message = "Üres kód." });
            if (req.Quantity < 1)
                return BadRequest(new { result = "InvalidInput", message = "A darabszám legalább 1." });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;

            var period = await _db.InventoryPeriods
                .FirstOrDefaultAsync(p => p.InventoryZoneId == req.InventoryZoneId && p.Status == "open");
            if (period == null)
                return BadRequest(new { result = "NoOpenPeriod", message = "A körzetben nincs nyitott leltári időszak." });

            var roomOk = await _db.Rooms.AnyAsync(r =>
                r.Id == req.RoomId && r.InventoryZoneId == req.InventoryZoneId);
            if (!roomOk)
                return BadRequest(new { result = "InvalidRoom", message = "Érvénytelen helyiség ebben a körzetben." });

            var matches = await _db.Assets
                .Where(a => a.Codes.Any(c => c.CodeValue == code))
                .Select(a => new
                {
                    Asset = a,
                    IsPrimary = a.Codes.Any(c => c.CodeValue == code && c.IsPrimary)
                })
                .ToListAsync();

            var candidates = matches.Any(m => m.IsPrimary)
                ? matches.Where(m => m.IsPrimary).ToList()
                : matches;

            if (candidates.Count == 0)
                return NotFound(new { result = "UnknownCode", message = $"Ismeretlen kód: {code}" });

            if (candidates.Count > 1)
            {
                var names = string.Join(", ", candidates.Take(3).Select(m => m.Asset.Name));
                return Conflict(new
                {
                    result = "AmbiguousCode",
                    message = $"A(z) {code} kód több eszközhöz is tartozik ({names}). Olvasd be az eszköz leltárszámát."
                });
            }

            var asset = candidates[0].Asset;

            var scan = new Scan
            {
                AssetId = asset.Id,
                InventoryPeriodId = period.Id,
                InventoryZoneId = req.InventoryZoneId,
                RoomId = req.RoomId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ScannedQuantity = req.Quantity,
                IsOtherZone = asset.InventoryZoneId != req.InventoryZoneId
            };
            _db.Scans.Add(scan);
            await _db.SaveChangesAsync();


            var scannedTotal = await _db.Scans
                .Where(s => s.AssetId == asset.Id && s.InventoryPeriodId == period.Id)
                .SumAsync(s => s.ScannedQuantity);

            // élő értesítés a klienseknek
            var item = await _db.Scans.Where(s => s.Id == scan.Id).Select(ToListItem).FirstAsync();
            await _hub.Clients.All.SendAsync(InventoryHub.ScanCreated, item);

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
                .Select(ToListItem)
                .ToListAsync();

            return Ok(list);
        }
    }
}