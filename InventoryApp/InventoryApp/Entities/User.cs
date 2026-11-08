namespace InventoryApp.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        // Törlés helyett inaktiválás
        public bool IsActive { get; set; } = true;

        // Ideiglenes (admin által generált) jelszó esetén igaz: ezzel a jelszóval
        // nem lehet belépni, csak egyszer, a saját jelszó beállítására használható.
        public bool MustChangePassword { get; set; }

        // Az ideiglenes jelszó lejárata (UTC). Lejárt jelszó helyett új kell az admintól.
        public DateTime? TempPasswordExpiresAt { get; set; }

        // Jelszócsere, szerepkör-módosítás vagy inaktiválás után megváltozik,
        // így a korábban kiadott tokenek azonnal érvénytelenné válnak.
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

        // Opcionális: egy User nem feltétlenül felelős senkiért.
        public int? ResponsiblePersonId { get; set; }
        public ResponsiblePerson? ResponsiblePerson { get; set; }
    }
}