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
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=inventory.db"));

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