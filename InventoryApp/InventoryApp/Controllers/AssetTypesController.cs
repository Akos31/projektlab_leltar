using InventoryApp.Data;
using InventoryApp.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryApp.Controllers
{
    public class AssetTypeRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssetTypesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AssetTypesController(AppDbContext db) => _db = db;

        // GET /api/assettypes
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var types = await _db.AssetTypes
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, AssetCount = t.Assets.Count })
                .ToListAsync();
            return Ok(types);
        }

        // POST /api/assettypes - csak admin
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Create([FromBody] AssetTypeRequest req)
        {
            var name = req.Name?.Trim() ?? "";
            if (name.Length == 0)
                return BadRequest(new { message = "A név nem lehet üres." });

            if (await _db.AssetTypes.AnyAsync(t => t.Name.ToLower() == name.ToLower()))
                return Conflict(new { message = $"Már van '{name}' nevű eszköztípus." });

            var type = new AssetType { Name = name };
            _db.AssetTypes.Add(type);
            await _db.SaveChangesAsync();
            return Ok(new { type.Id, type.Name });
        }
    }
}