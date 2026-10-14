using InventoryApp.Data;
using InventoryApp.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryApp.Controllers
{
    public class ZoneRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
    public class RoomRequest
    {
        public string RoomNumber { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ZonesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ZonesController(AppDbContext db) => _db = db;

        // Körzetek + az esetleg nyitott időszak 
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var zones = await _db.InventoryZones
                .Where(z => includeInactive || z.IsActive)
                .OrderBy(z => z.Code)
                .Select(z => new
                {
                    z.Id,
                    z.Code,
                    z.Name,
                    z.IsActive,
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
            var zone = await _db.InventoryZones.FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null)
                return NotFound(new { message = "Nincs ilyen körzet." });
            if (!zone.IsActive)
                return BadRequest(new { message = "Inaktív körzetben nem indítható leltári időszak." });
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

        // Egy körzet leltári időszakai (legújabb elöl)
        [HttpGet("{id:int}/periods")]
        public async Task<IActionResult> GetPeriods(int id)
        {
            var list = await _db.InventoryPeriods
                .Where(p => p.InventoryZoneId == id)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new
                {
                    p.Id,
                    p.StartDate,
                    p.EndDate,
                    p.Status,
                    ScanCount = p.Scans.Count
                })
                .ToListAsync();
            return Ok(list);
        }
        // Új körzet
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Create([FromBody] ZoneRequest req)
        {
            var code = req.Code?.Trim() ?? "";
            var name = req.Name?.Trim() ?? "";
            if (code.Length == 0)
                return BadRequest(new { message = "A körzet kódja nem lehet üres." });
            if (await _db.InventoryZones.AnyAsync(z => z.Code.ToLower() == code.ToLower()))
                return Conflict(new { message = $"Már van '{code}' kódú körzet." });

            var zone = new InventoryZone { Code = code, Name = name.Length == 0 ? code : name, IsActive = true };
            _db.InventoryZones.Add(zone);
            await _db.SaveChangesAsync();
            return Ok(new { zone.Id, zone.Code, zone.Name, zone.IsActive });
        }

        // Szerkesztés
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] ZoneRequest req)
        {
            var zone = await _db.InventoryZones.FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null)
                return NotFound(new { message = "Nincs ilyen körzet." });

            var code = req.Code?.Trim() ?? "";
            var name = req.Name?.Trim() ?? "";
            if (code.Length == 0)
                return BadRequest(new { message = "A körzet kódja nem lehet üres." });
            if (await _db.InventoryZones.AnyAsync(z => z.Id != id && z.Code.ToLower() == code.ToLower()))
                return Conflict(new { message = $"Már van '{code}' kódú körzet." });

            zone.Code = code;
            zone.Name = name.Length == 0 ? code : name;
            await _db.SaveChangesAsync();
            return Ok(new { zone.Id, zone.Code, zone.Name, zone.IsActive });
        }

        // Inaktívvá tétel (csak nyitott időszak nélkül)
        [HttpPost("{id:int}/deactivate")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var zone = await _db.InventoryZones.FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null)
                return NotFound(new { message = "Nincs ilyen körzet." });

            if (await _db.InventoryPeriods.AnyAsync(p => p.InventoryZoneId == id && p.Status == "open"))
                return Conflict(new { message = "A körzetnek nyitott leltári időszaka van, előbb zárd le." });

            zone.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { zone.Id, zone.IsActive });
        }

        [HttpPost("{id:int}/activate")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Activate(int id)
        {
            var zone = await _db.InventoryZones.FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null)
                return NotFound(new { message = "Nincs ilyen körzet." });

            zone.IsActive = true;
            await _db.SaveChangesAsync();
            return Ok(new { zone.Id, zone.IsActive });
        }
    }
}