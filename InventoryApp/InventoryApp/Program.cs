using System.Security.Claims;
using System.Text;
using InventoryApp.Data;
using InventoryApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + "Authorize" gomb a token beillesztéséhez
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Illeszd be a /api/auth/login által visszaadott tokent."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Hiányzik a connection string.")));

builder.Services.AddScoped<ExcelImportService>();

// --- JWT hitelesítés ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
          ?? throw new InvalidOperationException("Hiányzik a Jwt szekció az appsettings.json-ból.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("JWT HIBA: " + ctx.Exception);
                return Task.CompletedTask;
            },

            // Minden kérésnél ellenőrizzük, hogy a token még érvényes-e a felhasználó
            // jelenlegi állapotához képest: inaktiválás, szerepkör-változás vagy
            // jelszó-visszaállítás után a régi token azonnal használhatatlan.
            OnTokenValidated = async ctx =>
            {
                var idClaim = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var stampClaim = ctx.Principal?.FindFirstValue(TokenService.SecurityStampClaim);
                if (!int.TryParse(idClaim, out var userId) || stampClaim is null)
                {
                    ctx.Fail("Hiányos token.");
                    return;
                }

                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.IsActive, u.MustChangePassword, u.SecurityStamp })
                    .FirstOrDefaultAsync();

                if (user is null || !user.IsActive || user.MustChangePassword ||
                    user.SecurityStamp != stampClaim)
                {
                    ctx.Fail("A token már nem érvényes.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
var app = builder.Build();

// --- Adatbázis + kezdő felhasználók ---
using (var scope = app.Services.CreateScope())
{
    DbSeeder.Seed(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

// --- Middleware pipeline ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();   // ÚJ - mindig a UseAuthorization ELŐTT
app.UseAuthorization();
app.MapControllers();

app.Run();