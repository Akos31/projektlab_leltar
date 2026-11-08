namespace InventoryApp.Dtos
{
    public record LoginRequest(string Username, string Password);

    // Ha MustChangePassword igaz, a Token üres: előbb a /api/auth/change-password
    // végponton kell új jelszót beállítani az ideiglenes jelszóval.
    public record LoginResponse(
        string Token,
        DateTime ExpiresAt,
        string Username,
        string DisplayName,
        string Role,
        bool MustChangePassword = false);

    // Saját jelszó módosítása. Bejelentkezés nélkül is hívható (az ideiglenes
    // jelszó cseréjéhez), ezért a felhasználónevet és a jelenlegi jelszót is kéri.
    public record ChangePasswordRequest(string Username, string CurrentPassword, string NewPassword);
}
