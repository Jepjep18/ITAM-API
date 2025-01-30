using Microsoft.AspNetCore.Mvc;
using IT_ASSET.Models;
using IT_ASSET.Utilities; 
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IT_ASSET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.password))
            {
                return BadRequest("User data is invalid.");
            }

            user.password = PasswordHasher.HashPassword(user.password);
            user.date_created = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.password = null;

            return CreatedAtAction(nameof(GetUser), new { id = user.id }, user);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.password = null;
            return user;
        }
    }
}
