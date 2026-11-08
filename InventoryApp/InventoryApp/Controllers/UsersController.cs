using System.Security.Claims;
using InventoryApp.Data;
using InventoryApp.Dtos;
using InventoryApp.Entities;
using InventoryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    // Felhasználókezelés - csak adminisztrátornak
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        private int? CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        // GET /api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users.AsNoTracking()
                .OrderBy(u => u.Username)
                .ToListAsync();
            return Ok(users.Select(ToDto));
        }

        // GET /api/users/roles  - a választható szerepkörök
        [HttpGet("roles")]
        public IActionResult GetRoles() => Ok(Enum.GetNames<UserRole>());

        // POST /api/users
        // Új felhasználó ideiglenes jelszóval, amit első belépéskor meg kell változtatnia.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            var username = req.Username?.Trim() ?? "";
            var displayName = req.DisplayName?.Trim() ?? "";

            var error = ValidateUsername(username);
            if (error != null) return BadRequest(new { message = error });
            if (!TryParseRole(req.Role, out var role))
                return BadRequest(new { message = "Érvénytelen szerepkör." });
            if (await UsernameTakenAsync(username, exceptId: null))
                return Conflict(new { message = $"Már van '{username}' nevű felhasználó." });

            var user = new User
            {
                Username = username,
                DisplayName = displayName.Length == 0 ? username : displayName,
                Role = role,
                IsActive = true
            };
            var temp = PasswordService.AssignTemporary(user, out var expiresAt);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new TempPasswordResponse(ToDto(user), temp, expiresAt));
        }

        // PUT /api/users/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "Nincs ilyen felhasználó." });

            var username = req.Username?.Trim() ?? "";
            var displayName = req.DisplayName?.Trim() ?? "";

            var error = ValidateUsername(username);
            if (error != null) return BadRequest(new { message = error });
            if (!TryParseRole(req.Role, out var role))
                return BadRequest(new { message = "Érvénytelen szerepkör." });
            if (await UsernameTakenAsync(username, exceptId: id))
                return Conflict(new { message = $"Már van '{username}' nevű felhasználó." });

            if (id == CurrentUserId && user.Username != username)
                return BadRequest(new { message = "A saját felhasználónevedet nem módosíthatod." });

            // Kizárás elleni védelem
            bool losesAdmin = user.Role == UserRole.Adminisztrator && user.IsActive &&
                              (role != UserRole.Adminisztrator || !req.IsActive);
            if (losesAdmin && id == CurrentUserId)
                return BadRequest(new { message = "Saját magadat nem fokozhatod le és nem teheted inaktívvá." });
            if (losesAdmin && !await _db.Users.AnyAsync(u =>
                    u.Id != id && u.IsActive && u.Role == UserRole.Adminisztrator))
                return BadRequest(new { message = "Legalább egy aktív adminisztrátornak maradnia kell." });

            // Szerepkör-változás vagy inaktiválás: a régi tokenek azonnal érvénytelenek
            if (user.Role != role || user.IsActive != req.IsActive || user.Username != username)
                user.SecurityStamp = Guid.NewGuid().ToString("N");

            user.Username = username;
            user.DisplayName = displayName.Length == 0 ? username : displayName;
            user.Role = role;
            user.IsActive = req.IsActive;
            await _db.SaveChangesAsync();

            return Ok(ToDto(user));
        }

        // POST /api/users/5/reset-password
        // Elfelejtett jelszó: új, egyszer használatos ideiglenes jelszó generálása.
        // A régi jelszó azonnal megszűnik, a felhasználó a következő belépéskor
        // köteles saját jelszót megadni.
        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "Nincs ilyen felhasználó." });
            if (id == CurrentUserId)
                return BadRequest(new { message = "A saját jelszavadat a Fiók oldalon módosíthatod." });

            var temp = PasswordService.AssignTemporary(user, out var expiresAt);
            await _db.SaveChangesAsync();

            return Ok(new TempPasswordResponse(ToDto(user), temp, expiresAt));
        }

        // --- Segédmetódusok ---

        private static UserDto ToDto(User u) => new(
            u.Id, u.Username, u.DisplayName, u.Role.ToString(), u.IsActive,
            u.MustChangePassword, u.TempPasswordExpiresAt);

        private static string? ValidateUsername(string username)
        {
            if (username.Length < 3 || username.Length > 50)
                return "A felhasználónév 3–50 karakter hosszú legyen.";
            if (!username.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'))
                return "A felhasználónévben csak betű, szám, pont, aláhúzás és kötőjel lehet.";
            return null;
        }

        private static bool TryParseRole(string? value, out UserRole role) =>
            Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role);

        private Task<bool> UsernameTakenAsync(string username, int? exceptId)
        {
            var lower = username.ToLower();
            return _db.Users.AnyAsync(u => u.Id != exceptId && u.Username.ToLower() == lower);
        }
    }
}
