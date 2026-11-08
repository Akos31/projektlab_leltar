namespace InventoryApp.Dtos
{
    public record CreateUserRequest(string Username, string DisplayName, string Role);

    public record UpdateUserRequest(string Username, string DisplayName, string Role, bool IsActive);

    public record UserDto(
        int Id,
        string Username,
        string DisplayName,
        string Role,
        bool IsActive,
        bool MustChangePassword,
        DateTime? TempPasswordExpiresAt);

    // Létrehozáskor és jelszó-visszaállításkor: az ideiglenes jelszót csak ez az
    // egy válasz tartalmazza, a szerver sima szövegként sehol nem tárolja.
    public record TempPasswordResponse(UserDto User, string TemporaryPassword, DateTime ExpiresAt);
}
