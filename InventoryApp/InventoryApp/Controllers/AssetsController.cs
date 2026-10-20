using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ExcelImportService _importService;

        public AssetsController(AppDbContext db, ExcelImportService importService)
        {
            _db = db;
            _importService = importService;
        }

        // GET /api/assets?skip=0&take=100&search=...&status=...&type=...&zone=...
        // Lapozott, szûrt lista
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] string? zone = null)
        {
            skip = Math.Max(0, skip);
            take = Math.Clamp(take, 1, 200);

            IQueryable<Asset> query = _db.Assets.AsNoTracking();

            // Keresés: név, eszközazonosító vagy bármelyik kód (pl. leltárszám)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(a =>
                    EF.Functions.Like(a.Name, pattern) ||
                    EF.Functions.Like(a.SourceId, pattern) ||
                    a.Codes.Any(c => EF.Functions.Like(c.CodeValue, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(a => a.AssetType!.Name == type);

            if (!string.IsNullOrWhiteSpace(zone))
                query = query.Where(a => a.InventoryZone!.Code == zone);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(a => a.Id)          // stabil sorrend kell a lapozáshoz
                .Skip(skip)
                .Take(take)
                .Select(a => new
                {
                    a.Id,
                    a.SourceId,
                    a.Name,
                    a.ExpectedQuantity,
                    a.Status,
                    Type = a.AssetType!.Name,
                    Zone = a.InventoryZone!.Code,
                    InventoryNumber = a.Codes
                        .Where(c => c.IsPrimary)
                        .Select(c => c.CodeValue)
                        .FirstOrDefault(),
                                        OpenPeriodId = a.InventoryZone!.InventoryPeriods
                        .Where(p => p.Status == "open")
                        .Select(p => (int?)p.Id)
                        .FirstOrDefault(),
                                        ScannedTotal = a.Scans
                        .Where(s => s.InventoryPeriod.Status == "open")
                        .Sum(s => (int?)s.ScannedQuantity) ?? 0,
                    HasWrongZoneScan = a.Scans.Any(s => s.InventoryPeriod.Status == "open" && s.IsOtherZone),
                    AccessoryCount = a.Accessories.Count
                })
                .ToListAsync();

            return Ok(new { Items = items, TotalCount = totalCount });
        }

        // GET /api/assets/filters
        // A legördülõ szûrõk lehetséges értékei
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var statuses = await _db.Assets
                .Select(a => a.Status).Distinct().OrderBy(s => s).ToListAsync();

            var types = await _db.Assets
                .Select(a => a.AssetType!.Name).Distinct().OrderBy(n => n).ToListAsync();

            var zones = await _db.Assets
                .Select(a => a.InventoryZone!.Code).Distinct().OrderBy(c => c).ToListAsync();

            return Ok(new { Statuses = statuses, Types = types, Zones = zones });
        }
        // GET /api/assets/5
        // ID alapu kereses
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var asset = LoadAssetQuery().FirstOrDefault(a => a.Id == id);

            if (asset is null) return NotFound();
            return Ok(ToDto(asset));
        }

        // GET /api/assets/inventory/3038981
        // Leltarszam alapu kereses
        [HttpGet("inventory/{inventoryNumber}")]
        public IActionResult GetByInventoryNumber(string inventoryNumber)
        {
            var asset = LoadAssetQuery()
                .FirstOrDefault(a => a.Codes.Any(c =>
                    c.CodeType == "InventoryNumber" && c.CodeValue == inventoryNumber));

            if (asset is null)
                return NotFound($"No asset found with inventory number '{inventoryNumber}'.");

            return Ok(ToDto(asset));
        }

        // --- Segedmetodusok ---

        private IQueryable<Asset> LoadAssetQuery() =>
            _db.Assets
                .AsNoTracking()
                .Include(a => a.AssetType)
                .Include(a => a.InventoryZone)
                .Include(a => a.Codes)
                .Include(a => a.Accessories);

        private static object ToDto(Asset asset) => new
        {
            asset.Id,
            asset.SourceId,
            asset.Name,
            asset.ExpectedQuantity,
            asset.Status,
            Type = asset.AssetType?.Name,
            Zone = asset.InventoryZone?.Code,
            InventoryNumber = asset.Codes
                .Where(c => c.IsPrimary)
                .Select(c => c.CodeValue)
                .FirstOrDefault(),
            Codes = asset.Codes.Select(c => new { c.CodeType, c.CodeValue, c.IsPrimary }),
            Accessories = asset.Accessories.Select(ac => new
            {
                ac.Name,
                ac.Quantity,
                ac.SubNumber,
                ac.VerifiedDirectly
            })
        };
        // POST /api/assets/import
        // Excel feltoltese - csak admin
        [HttpPost("import")]
        [Authorize(Roles = Roles.Admin)]

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
                    result.SkippedExisting,
                    result.SkippedInvalid
                });
            }
            finally
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }
}
