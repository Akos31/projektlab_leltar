using InventoryApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            // Létrehozza/frissíti az adatbázist, így nem kell külön Update-Database
            db.Database.Migrate();

            if (db.Users.Any())
                return;

            db.Users.AddRange(
                NewUser("admin", "Admin123!", "Rendszergazda", UserRole.Adminisztrator),
                NewUser("leltarozo", "Leltar123!", "Teszt Leltározó", UserRole.Leltarozo),
                NewUser("kiertekelo", "Kiert123!", "Teszt Kiértékelő", UserRole.Kiertekelo));

            db.SaveChanges();
        }

        private static User NewUser(string username, string password, string displayName, UserRole role) => new()
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = displayName,
            Role = role,
            IsActive = true
        };
    }
}