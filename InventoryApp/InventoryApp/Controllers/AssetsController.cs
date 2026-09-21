using InventoryApp.Data;
using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ExcelImportService _importService;

        public AssetsController(AppDbContext db, ExcelImportService importService)
        {
            _db = db;
            _importService = importService;
        }

        // GET /api/assets
        // Minden kilistazasa
        [HttpGet]
        public IActionResult GetAll()
        {
            var assets = _db.Assets
                .Include(a => a.AssetType)
                .Include(a => a.InventoryZone)
                .Include(a => a.Codes)
                .Include(a => a.Accessories)
                .Select(a => new
                {
                    a.Id,
                    a.SourceId,
                    a.Name,
                    a.ExpectedQuantity,
                    a.Status,
                    Type = a.AssetType!.Name,
                    Zone = a.InventoryZone!.Code,
                    Codes = a.Codes.Select(c => new { c.CodeType, c.CodeValue, c.IsPrimary }),
                    AccessoryCount = a.Accessories.Count
                })
                .ToList();

            return Ok(assets);
        }

        // GET /api/assets/{id}
        // ID alapu kereses
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var asset = _db.Assets
                .Include(a => a.AssetType)
                .Include(a => a.InventoryZone)
                .Include(a => a.Codes)
                .Include(a => a.Accessories)
                .FirstOrDefault(a => a.Id == id);

            if (asset is null) return NotFound();
            return Ok(asset);
        }

        // GET /api/assets/{inventoryNumber}
        // Leltarszam alapu kereses
        [HttpGet("{inventoryNumber}")]
        public IActionResult GetByInventoryNumber(string inventoryNumber)
        {
            var asset = _db.Assets
                .Include(a => a.AssetType)
                .Include(a => a.InventoryZone)
                .Include(a => a.Codes)
                .Include(a => a.Accessories)
                .FirstOrDefault(a => a.Codes.Any(c =>
                    c.CodeType == "InventoryNumber" && c.CodeValue == inventoryNumber));

            if (asset is null)
                return NotFound($"No asset found with inventory number '{inventoryNumber}'.");

            return Ok(new
            {
                asset.Id,
                asset.SourceId,
                asset.Name,
                asset.ExpectedQuantity,
                asset.Status,
                Type = asset.AssetType?.Name,
                Zone = asset.InventoryZone?.Code,
                Codes = asset.Codes.Select(c => new { c.CodeType, c.CodeValue, c.IsPrimary }),
                Accessories = asset.Accessories.Select(ac => new
                {
                    ac.Name,
                    ac.Quantity,
                    ac.SubNumber,
                    ac.VerifiedDirectly
                })
            });
        }
            // POST /api/assets/import
            // Excel feltoltese
            [HttpPost("import")]

        public async Task<IActionResult> Import(IFormFile file, [FromForm] string zoneName)
        {
            if (file is null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
            await using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                var result = _importService.ImportFromFile(tempPath, zoneName);
                return Ok(new
                {
                    result.Imported,
                    result.SkippedExisting
                });
            }
            finally
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }
}
