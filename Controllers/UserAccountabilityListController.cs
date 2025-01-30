using IT_ASSET.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IT_ASSET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountabilityListController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserAccountabilityListController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UserAccountabilityList
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAccountabilityList>>> GetAllUserAccountabilityLists()
        {
            return await _context.user_accountability_lists.ToListAsync();
        }

        // GET: api/UserAccountabilityList/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAccountabilityList>> GetUserAccountabilityListById(int id)
        {
            var userAccountabilityList = await _context.user_accountability_lists.FindAsync(id);

            if (userAccountabilityList == null)
            {
                return NotFound();
            }

            return userAccountabilityList;
        }
    }
}