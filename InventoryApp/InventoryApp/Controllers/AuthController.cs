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
            if (user is null || !user.IsActive ||
                !BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, user.PasswordHash))
            {
                return Unauthorized("Hibás felhasználónév vagy jelszó.");
            }

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