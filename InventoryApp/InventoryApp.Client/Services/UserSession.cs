using InventoryApp.Client.Models;

namespace InventoryApp.Client.Services;

public class UserSession
{
    // Ugyanazok a nevek, mint a backend UserRole enumjában
    public const string Admin = "Adminisztrator";
    public const string Leltarozo = "Leltarozo";
    public const string Kiertekelo = "Kiertekelo";

    public LoginResponse? Current { get; private set; }

    public bool IsLoggedIn => Current is not null;
    public string Role => Current?.Role ?? string.Empty;

    // Jogosultságok egy helyen
    public bool CanScan => Role is Admin or Leltarozo;
    public bool CanImport => Role is Admin;
    public bool CanViewReports => Role is Admin or Kiertekelo;
    public bool CanManageZones => Role is Admin;

    public string RoleDisplayName => Role switch
    {
        Admin => "Adminisztrátor",
        Leltarozo => "Leltározó",
        Kiertekelo => "Kiértékelő",
        _ => Role
    };

    public void SignIn(LoginResponse login) => Current = login;
    public void SignOut() => Current = null;
}