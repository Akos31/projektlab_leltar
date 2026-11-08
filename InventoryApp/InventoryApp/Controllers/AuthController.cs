using System.Security.Claims;
using InventoryApp.Data;
using InventoryApp.Dtos;
using InventoryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokens;

        private const string InactiveMessage =
            "A fiókod inaktív, fordulj az adminisztrátorhoz.";

        public AuthController(AppDbContext db, TokenService tokens)
        {
            _db = db;
            _tokens = tokens;
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login(LoginRequest request)
        {
            var username = request.Username?.Trim() ?? string.Empty;
            var user = _db.Users.FirstOrDefault(u => u.Username == username);

            // Ugyanaz az üzenet minden esetben, hogy ne derüljön ki, létezik-e a felhasználó
            if (user is null ||
                !PasswordService.Verify(request.Password ?? string.Empty, user.PasswordHash))
            {
                return Unauthorized("Hibás felhasználónév vagy jelszó.");
            }

            // Csak helyes jelszó után áruljuk el, hogy a fiók inaktív
            if (!user.IsActive)
                return Unauthorized(InactiveMessage);

            if (user.MustChangePassword)
            {
                if (user.TempPasswordExpiresAt is { } exp && exp < DateTime.UtcNow)
                    return Unauthorized("Az ideiglenes jelszó lejárt, kérj újat az adminisztrátortól.");

                // Ideiglenes jelszóval nem adunk tokent: a kliensnek új jelszót kell kérnie.
                return Ok(new LoginResponse(
                    string.Empty, default, user.Username, user.DisplayName, user.Role.ToString(),
                    MustChangePassword: true));
            }

            var (token, expiresAt) = _tokens.CreateToken(user);

            return Ok(new LoginResponse(
                token, expiresAt, user.Username, user.DisplayName, user.Role.ToString()));
        }

        // POST /api/auth/change-password
        // Saját jelszó módosítása. Az ideiglenes jelszó cseréjéhez bejelentkezés nélkül
        // is hívható (felhasználónév + jelenlegi jelszó). Sikeres csere után új tokent ad,
        // a korábban kiadott tokenek érvényüket vesztik.
        [HttpPost("change-password")]
        [AllowAnonymous]
        public IActionResult ChangePassword(ChangePasswordRequest request)
        {
            var username = request.Username?.Trim() ?? string.Empty;
            var user = _db.Users.FirstOrDefault(u => u.Username == username);

            // A felhasználónevet mindig a kliens adja (belépési adat vagy a munkamenet),
            // így a felhasználó szemszögéből csak a jelenlegi jelszó lehet hibás.
            // Nem létező felhasználónál is ugyanez az üzenet, hogy ne lehessen kifürkészni.
            if (user is null ||
                !PasswordService.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
            {
                return Unauthorized("A jelenlegi jelszó hibás.");
            }

            if (!user.IsActive)
                return Unauthorized(InactiveMessage);

            if (user.MustChangePassword && user.TempPasswordExpiresAt is { } exp && exp < DateTime.UtcNow)
                return Unauthorized("Az ideiglenes jelszó lejárt, kérj újat az adminisztrátortól.");

            var error = PasswordService.ValidateNew(request.NewPassword, user.Username);
            if (error is null && request.NewPassword == request.CurrentPassword)
                error = "Az új jelszó nem egyezhet a jelenlegivel.";
            if (error is not null)
                return BadRequest(new { message = error });

            user.PasswordHash = PasswordService.Hash(request.NewPassword);
            user.MustChangePassword = false;
            user.TempPasswordExpiresAt = null;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            _db.SaveChanges();

            var (token, expiresAt) = _tokens.CreateToken(user);
            return Ok(new LoginResponse(
                token, expiresAt, user.Username, user.DisplayName, user.Role.ToString()));
        }

        // GET /api/auth/me  - a token alapján visszaadja, ki van bejelentkezve
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me() => Ok(new
        {
            Username = User.Identity?.Name,
            DisplayName = User.FindFirstValue("displayName"),
            Role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}