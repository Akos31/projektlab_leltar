namespace InventoryApp.Client.Models;

// GET api/users egy eleme
public class UserItem
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? TempPasswordExpiresAt { get; set; }

    // --- Megjelenítéshez ---

    public string RoleText => RoleToText(Role);

    public string Subtitle => $"@{Username} · {RoleText}";

    public string StatusText =>
        !IsActive ? "Inaktív"
        : MustChangePassword
            ? TempPasswordExpiresAt is { } exp && exp < DateTime.UtcNow
                ? "Ideiglenes jelszó lejárt"
                : $"Ideiglenes jelszó (lejár: {TempPasswordExpiresAt?.ToLocalTime():yyyy.MM.dd HH:mm})"
            : "Aktív";

    public Color StatusColor =>
        !IsActive ? Colors.Gray
        : MustChangePassword ? Color.FromArgb("#EF6C00")
        : Color.FromArgb("#2E7D32");

    public static string RoleToText(string role) => role switch
    {
        "Adminisztrator" => "Adminisztrátor",
        "Leltarozo" => "Leltározó",
        "Kiertekelo" => "Kiértékelő",
        _ => role
    };
}

// Létrehozás / jelszó-visszaállítás válasza: az ideiglenes jelszó csak itt látszik
public class TempPasswordResult
{
    public UserItem User { get; set; } = new();
    public string TemporaryPassword { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
