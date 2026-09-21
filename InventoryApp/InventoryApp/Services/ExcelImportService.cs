using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using InventoryApp.Data;
using InventoryApp.Entities;

namespace InventoryApp.Services
{
    public class ExcelImportService
    {
        private readonly AppDbContext _db;

        // Known non-values that show up in the "Gyártási szám" column and are
        // not real serial numbers. Extend this list as you find more.
        private static readonly HashSet<string> KnownNonSerialValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "-", "NINCS", "BÚTOR"
        };

        public ExcelImportService(AppDbContext db)
        {
            _db = db;
        }

        public ImportResult ImportFromFile(string filePath, string inventoryZoneNameHint)
        {
            var result = new ImportResult();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            // Read the header row so we access columns by name, not by
            // hard-coded position - more resilient if columns get reordered.
            var headerRow = worksheet.Row(1);
            var columnIndex = headerRow.CellsUsed()
                .ToDictionary(c => c.GetString().Trim(), c => c.Address.ColumnNumber);

            string Get(IXLRow row, string header)
            {
                if (!columnIndex.TryGetValue(header, out var col)) return string.Empty;
                return row.Cell(col).GetString().Trim();
            }

            // --- Step 1: read raw rows and skip the "summary" rows ---
            // A summary row has no asset id / name / inventory number at all -
            // only the financial columns are filled in.
            var dataRows = worksheet.RowsUsed()
                .Skip(1) // header
                .Where(row =>
                {
                    var hasName = !string.IsNullOrWhiteSpace(Get(row, "Eszköz megnevezése"));
                    var hasInventoryNumber = !string.IsNullOrWhiteSpace(Get(row, "Leltárszám"));
                    return hasName || hasInventoryNumber;
                })
                .ToList();

            // --- Step 2: group by the source asset id ("Eszköz") ---
            var groups = dataRows.GroupBy(row => Get(row, "Eszköz"));

            foreach (var group in groups)
            {
                var sourceId = group.Key;
                if (string.IsNullOrWhiteSpace(sourceId)) continue;

                // Skip if this asset was already imported before (re-running
                // the import should not create duplicates).
                if (_db.Assets.Any(a => a.SourceId == sourceId))
                {
                    result.SkippedExisting++;
                    continue;
                }

                var mainRow = group.FirstOrDefault(r => Get(r, "Alszám") == "0" || Get(r, "Alszám") == "");
                if (mainRow is null) continue;

                // --- Step 3: build the Asset ---
                var asset = new Asset
                {
                    SourceId = sourceId,
                    Name = Get(mainRow, "Eszköz megnevezése"),
                    ExpectedQuantity = ParseInt(Get(mainRow, "Mennyiség"), fallback: 1),
                    Status = "active",
                    ActivationDate = ParseDate(Get(mainRow, "Aktiválás dátuma")),
                    PurchaseValue = ParseDecimal(Get(mainRow, "Besz.ért.")),
                    NetBookValue = ParseDecimal(Get(mainRow, "K.sz.ért"))
                };

                // --- Step 4: asset codes (primary = Leltárszám) ---
                AddCodeIfPresent(asset, Get(mainRow, "Leltárszám"), "InventoryNumber", isPrimary: true);
                AddCodeIfPresent(asset, Get(mainRow, "Eredeti eszköz"), "OriginalAssetNumber", isPrimary: false);
                AddCodeIfPresent(asset, Get(mainRow, "Adóhivatal"), "TaxReference", isPrimary: false);

                // --- Step 5: accessories (Alszám > 0 rows) ---
                foreach (var subRow in group)
                {
                    var subNumberText = Get(subRow, "Alszám");
                    if (!int.TryParse(subNumberText, out var subNumber) || subNumber <= 0) continue;

                    asset.Accessories.Add(new Accessory
                    {
                        Name = Get(subRow, "Eszköz megnevezése"),
                        Quantity = ParseInt(Get(subRow, "Mennyiség"), fallback: 1),
                        SubNumber = subNumber,
                        VerifiedDirectly = false
                    });
                }

                // --- Step 6: asset type (create-or-reuse, no fuzzy matching) ---
                var typeName = NormalizeTypeName(asset.Name);
                var assetType = _db.AssetTypes.Local.FirstOrDefault(t => t.Name == typeName)
                                 ?? _db.AssetTypes.FirstOrDefault(t => t.Name == typeName);
                if (assetType is null)
                {
                    assetType = new AssetType { Name = typeName };
                    _db.AssetTypes.Add(assetType);
                }
                asset.AssetType = assetType;

                // --- Step 7: inventory zone (from "Telephely") ---
                var telephely = Get(mainRow, "Telephely");
                var zoneCode = MapTelephelyToZoneCode(telephely);
                var zone = _db.InventoryZones.Local.FirstOrDefault(z => z.Code == zoneCode)
                           ?? _db.InventoryZones.FirstOrDefault(z => z.Code == zoneCode);
                if (zone is null)
                {
                    zone = new InventoryZone { Code = zoneCode, Name = inventoryZoneNameHint };
                    _db.InventoryZones.Add(zone);
                }
                asset.InventoryZone = zone;

                _db.Assets.Add(asset);
                result.Imported++;
            }

            _db.SaveChanges();
            return result;
        }

        // --- Helpers ---

        private void AddCodeIfPresent(Asset asset, string value, string codeType, bool isPrimary)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            asset.Codes.Add(new AssetCode
            {
                CodeValue = value,
                CodeType = codeType,
                IsPrimary = isPrimary
            });
        }

        private static string NormalizeTypeName(string rawName)
        {
            // Deliberately simple: trim + uppercase only. We do NOT try to
            // auto-correct typos (e.g. "TÁRGYALÓSZÉK" vs "TÁRGYALŐSZÉK") -
            // near-duplicates are merged later by an administrator.
            return rawName.Trim().ToUpperInvariant();
        }

        private static string MapTelephelyToZoneCode(string telephelyRaw)
        {
            // Source values look like "2610000000" / "2620000000" -
            // the meaningful zone code is the leading digits (e.g. "261").
            if (string.IsNullOrWhiteSpace(telephelyRaw)) return "UNKNOWN";

            if (double.TryParse(telephelyRaw, out var value))
            {
                var shortened = (long)(value / 10_000_000);
                return shortened.ToString();
            }

            return telephelyRaw;
        }

        private static bool IsKnownNonSerial(string value) =>
            string.IsNullOrWhiteSpace(value) || KnownNonSerialValues.Contains(value);

        private static int ParseInt(string text, int fallback)
        {
            if (double.TryParse(text, out var d)) return (int)d;
            return fallback;
        }

        private static decimal? ParseDecimal(string text)
        {
            if (decimal.TryParse(text, out var d)) return d;
            return null;
        }

        private static DateTime? ParseDate(string text)
        {
            if (DateTime.TryParse(text, out var d)) return d;
            return null;
        }
    }

    public class ImportResult
    {
        public int Imported { get; set; }
        public int SkippedExisting { get; set; }
    }
}
