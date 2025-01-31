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

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUserAccountabilityListById(int id)
        {
            try
            {
                // First, fetch the user accountability list without the owner details
                var userAccountabilityList = await _context.user_accountability_lists
                    .Where(u => u.id == id)
                    .Select(u => new
                    {
                        u.id,
                        u.accountability_code,
                        u.tracking_code,
                        u.owner_id,
                        u.asset_ids,
                        u.computer_ids
                    })
                    .FirstOrDefaultAsync();

                if (userAccountabilityList == null)
                {
                    return NotFound();
                }

                // Retrieve the owner details if owner_id is not null
                var owner = userAccountabilityList.owner_id != null
                    ? await _context.Users
                        .Where(user => user.id == userAccountabilityList.owner_id)
                        .Select(user => new
                        {
                            user.id,
                            user.name,
                            user.company,
                            user.department,
                            user.employee_id
                        })
                        .FirstOrDefaultAsync()
                    : null;

                // Pre-process asset_ids and computer_ids (split and parse them into a list of integers)
                var assetIds = userAccountabilityList.asset_ids.Split(',').Select(int.Parse).ToList();
                var computerIds = userAccountabilityList.computer_ids.Split(',').Select(int.Parse).ToList();

                // Retrieve the assets
                var assets = await _context.Assets
                    .Where(a => assetIds.Contains(a.id))
                    .Select(a => new
                    {
                        a.id,
                        a.type,
                        a.asset_barcode,
                        a.brand,
                        a.model,
                        a.ram,
                        a.ssd,
                        a.hdd,
                        a.gpu,
                        a.size,
                        a.color,
                        a.serial_no,
                        a.po,
                        a.warranty,
                        a.cost,
                        a.remarks,
                        a.li_description,
                        a.history,
                        a.asset_image,
                        a.owner_id,
                        a.is_deleted,
                        a.date_created,
                        a.date_modified
                    })
                    .ToListAsync();

                // Retrieve the computers
                var computers = await _context.computers
                    .Where(c => computerIds.Contains(c.id))
                    .Select(c => new
                    {
                        c.id,
                        c.type,
                        c.asset_barcode,
                        c.brand,
                        c.model,
                        c.ram,
                        c.ssd,
                        c.hdd,
                        c.gpu,
                        c.size,
                        c.color,
                        c.serial_no,
                        c.po,
                        c.warranty,
                        c.cost,
                        c.remarks,
                        c.li_description,
                        c.history,
                        c.asset_image,
                        c.owner_id,
                        c.is_deleted,
                        c.date_created,
                        c.date_modified
                    })
                    .ToListAsync();

                // Return the result
                return Ok(new
                {
                    userAccountabilityList,
                    owner,
                    assets,
                    computers
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving user accountability list: {ex.Message}");
            }
        }


    }
}