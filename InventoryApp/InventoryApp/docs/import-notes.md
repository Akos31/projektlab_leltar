# Excel import - starter code

## 1. New project

```
dotnet new webapi -n InventoryApp
cd InventoryApp
```

Copy the `Entities`, `Data`, and `Services` folders into the project root.

## 2. Install packages

```
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package ClosedXML
```

(SQLite is a good starting point for development - no server setup needed.
Because everything goes through EF Core, swapping to SQL Server or
PostgreSQL later is just a different `UseSqlite(...)` / `UseSqlServer(...)`
call plus a new migration - the entities and the import service don't change.)

## 3. Register the DbContext (in `Program.cs`)

```csharp
using InventoryApp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=inventory.db"));

builder.Services.AddScoped<InventoryApp.Services.ExcelImportService>();

// ... existing controller/swagger setup ...

var app = builder.Build();
app.Run();
```

## 4. Create and apply the migration

```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This creates `inventory.db` with all the tables from the ER diagram.

## 5. Try the import

Quickest way to test without building a controller yet - add this
temporarily in `Program.cs`, right after `var app = builder.Build();`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var importer = new InventoryApp.Services.ExcelImportService(db);

    var result = importer.ImportFromFile("261_lista_20260909.XLSX", "261-es telephely");
    Console.WriteLine($"Imported: {result.Imported}, skipped (already existed): {result.SkippedExisting}");
}
```

Run it, check the console output, then open `inventory.db` with a SQLite
browser (e.g. "DB Browser for SQLite") to see the Assets, AssetCodes,
Accessories and AssetTypes tables populated.

## What this does NOT do yet

- No API endpoints (controllers) - this is just the import logic itself
- No merging UI for duplicate asset types
- No room/scan/inventory period logic - those come later with the actual
  stocktaking flow
- Minimal error handling - if a required column header is missing or a row
  is malformed in an unexpected way, this will currently throw or silently
  skip; worth hardening once you've tested against both sample files

## Next reasonable step

Run this against both `261_lista_20260909.XLSX` and `262_lista_20260909.XLSX`
and check:
- Does `Assets.Count()` roughly match the number of unique "Eszköz" values
  we found earlier (1063 and 1793)?
- Do the accessory counts look right for a few known multi-part assets
  (e.g. source id 3021346)?
- Are there exactly two InventoryZones after importing both files?
