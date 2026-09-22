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

        // Opcionális: egy User nem feltétlenül felelős senkiért.
        public int? ResponsiblePersonId { get; set; }
        public ResponsiblePerson? ResponsiblePerson { get; set; }
    }
}