namespace InventoryApp.Entities
{
    public enum UserRole
    {
        Leltarozo,
        Kiertekelo,
        Adminisztrator
    }

    public static class Roles
    {
        public const string Leltarozo = nameof(UserRole.Leltarozo);
        public const string Kiertekelo = nameof(UserRole.Kiertekelo);
        public const string Admin = nameof(UserRole.Adminisztrator);
    }
}