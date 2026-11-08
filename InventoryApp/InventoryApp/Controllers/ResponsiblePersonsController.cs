using InventoryApp.Data;
using InventoryApp.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryApp.Controllers
{
    public class ResponsiblePersonRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ResponsiblePersonsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ResponsiblePersonsController(AppDbContext db) => _db = db;

        // GET /api/responsiblepersons
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var persons = await _db.ResponsiblePersons
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();
            return Ok(persons);
        }

        // POST /api/responsiblepersons - csak admin
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Create([FromBody] ResponsiblePersonRequest req)
        {
            var name = req.Name?.Trim() ?? "";
            if (name.Length == 0)
                return BadRequest(new { message = "A név nem lehet üres." });

            if (await _db.ResponsiblePersons.AnyAsync(p => p.Name.ToLower() == name.ToLower()))
                return Conflict(new { message = $"Már van '{name}' nevű felelős személy." });

            var person = new ResponsiblePerson { Name = name };
            _db.ResponsiblePersons.Add(person);
            await _db.SaveChangesAsync();
            return Ok(new { person.Id, person.Name });
        }
    }
}