# Leltározó és leltárkezelő alkalmazás

Kliens–szerver alapú rendszer egyetemi eszközök nyilvántartására és
leltározására. Projekt Labor (VEMIINB336PL), 3. csoport: Németh Márton,
Horváth Kristóf, Kelemen Ákos.

## Jelenlegi állapot

Ez a szerveroldal (ASP.NET Core Web API) kiindulási változata:
adatmodell (EF Core entitások), adatbázis-kapcsolat, és az Excel
forrásállományok importálását végző szolgáltatás. A kliens (.NET MAUI)
és a leltározási folyamat még nem része ennek a verziónak.

Az import-logika döntéseiről és a forrásfájlok szerkezetéről bővebben:
[`docs/import-notes.md`](docs/import-notes.md).

## Futtatás

Előfeltétel: [.NET 9 SDK](https://dotnet.microsoft.com/download).

```
dotnet restore
dotnet ef database update
dotnet run
```

Ez létrehozza az `inventory.db` SQLite adatbázist, majd elindítja az API-t.
A böngésző automatikusan megnyitja a Swagger felületet
(`https://localhost:7080/swagger`), ahol az összes végpont kipróbálható.

Ha az `dotnet ef` parancs nem található:

```
dotnet tool install --global dotnet-ef
```

## Végpontok (jelenleg)

| Végpont | Leírás |
|---|---|
| `GET /api/assets` | Az összes importált eszköz listázása |
| `GET /api/assets/{id}` | Egy eszköz részletei |
| `POST /api/assets/import` | Excel forrásfájl importálása (`file` mezőben a fájl, `zoneName` mezőben a leltárkörzet neve) |

## Projektstruktúra

```
Entities/     - EF Core adatmodell (Asset, AssetCode, Accessory, ...)
Data/         - AppDbContext
Services/     - ExcelImportService
Controllers/  - API végpontok
docs/         - kiegészítő dokumentáció
```

## Technológiák

- ASP.NET Core Web API (.NET 9)
- Entity Framework Core + SQLite (fejlesztéshez; a végleges adatbázis-
  termék még nyitott kérdés, EF Core miatt könnyen cserélhető)
- ClosedXML (Excel-import)
- .NET MAUI (kliens — még nincs a repóban)

## Csapat és munkamegosztás

*(töltsétek ki: ki miért felelt ebben a fázisban)*
