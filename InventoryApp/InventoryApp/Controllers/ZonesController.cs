using InventoryApp.Data;
using InventoryApp.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class RoomRequest
    {
        public string RoomNumber { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ZonesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ZonesController(AppDbContext db) => _db = db;

        // Körzetek + az esetleg nyitott időszak azonosítója
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var zones = await _db.InventoryZones
                .OrderBy(z => z.Code)
                .Select(z => new
                {
                    z.Id,
                    z.Code,
                    z.Name,
                    OpenPeriodId = z.InventoryPeriods
                        .Where(p => p.Status == "open")
                        .Select(p => (int?)p.Id)
                        .FirstOrDefault()
                })
                .ToListAsync();
            return Ok(zones);
        }

        // Nyitott időszak indítása (ha már van, azt adja vissza)
        [HttpPost("{id:int}/open-period")]
        public async Task<IActionResult> OpenPeriod(int id)
        {
            if (!await _db.InventoryZones.AnyAsync(z => z.Id == id))
                return NotFound(new { message = "Nincs ilyen körzet." });

            var open = await _db.InventoryPeriods
                .FirstOrDefaultAsync(p => p.InventoryZoneId == id && p.Status == "open");
            if (open != null)
                return Ok(new { open.Id, open.StartDate, open.Status });

            var period = new InventoryPeriod
            {
                InventoryZoneId = id,
                StartDate = DateTime.UtcNow,
                Status = "open"
            };
            _db.InventoryPeriods.Add(period);
            await _db.SaveChangesAsync();
            return Ok(new { period.Id, period.StartDate, period.Status });
        }

        [HttpPost("{id:int}/close-period")]
        public async Task<IActionResult> ClosePeriod(int id)
        {
            var open = await _db.InventoryPeriods
                .FirstOrDefaultAsync(p => p.InventoryZoneId == id && p.Status == "open");
            if (open == null)
                return NotFound(new { message = "Nincs nyitott időszak ebben a körzetben." });

            open.Status = "closed";
            open.EndDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { open.Id, open.Status, open.EndDate });
        }

        // Egy körzet helyiségei
        [HttpGet("{id:int}/rooms")]
        public async Task<IActionResult> GetRooms(int id)
        {
            var rooms = await _db.Rooms
                .Where(r => r.InventoryZoneId == id)
                .OrderBy(r => r.RoomNumber)
                .Select(r => new { r.Id, r.RoomNumber })
                .ToListAsync();
            return Ok(rooms);
        }

        // Új helyiség felvétele (a helyiségeknek nincs forrásadata, itt jönnek létre)
        [HttpPost("{id:int}/rooms")]
        public async Task<IActionResult> AddRoom(int id, [FromBody] RoomRequest req)
        {
            var number = req.RoomNumber?.Trim() ?? "";
            if (number.Length == 0)
                return BadRequest(new { message = "A helyiség száma nem lehet üres." });
            if (!await _db.InventoryZones.AnyAsync(z => z.Id == id))
                return NotFound(new { message = "Nincs ilyen körzet." });

            var existing = await _db.Rooms.FirstOrDefaultAsync(r =>
                r.InventoryZoneId == id && r.RoomNumber.ToLower() == number.ToLower());
            if (existing != null)
                return Ok(new { existing.Id, existing.RoomNumber });   // nincs duplikáció

            var room = new Room { InventoryZoneId = id, RoomNumber = number };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();
            return Ok(new { room.Id, room.RoomNumber });
        }
    }
}