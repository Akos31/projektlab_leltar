using InventoryApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<AssetCode> AssetCodes => Set<AssetCode>();
        public DbSet<Accessory> Accessories => Set<Accessory>();
        public DbSet<AssetType> AssetTypes => Set<AssetType>();
        public DbSet<InventoryZone> InventoryZones => Set<InventoryZone>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<InventoryPeriod> InventoryPeriods => Set<InventoryPeriod>();
        public DbSet<Scan> Scans => Set<Scan>();
        public DbSet<ResponsiblePerson> ResponsiblePersons => Set<ResponsiblePerson>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Two assets can't share the same source Excel identifier
            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.SourceId)
                .IsUnique();

            // The same code value should not appear twice under the same type
            // (helps catch accidental double-imports)
            modelBuilder.Entity<AssetCode>()
                .HasIndex(c => new { c.CodeType, c.CodeValue });

            modelBuilder.Entity<AssetType>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<InventoryZone>()
                .HasIndex(z => z.Code)
                .IsUnique();

            // Keep decimal precision explicit for the financial fields
            modelBuilder.Entity<Asset>()
                .Property(a => a.PurchaseValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Asset>()
                .Property(a => a.NetBookValue)
                .HasPrecision(18, 2);
        }
    }
}
