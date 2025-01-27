using IT_ASSET.DTOs;
using IT_ASSET.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IT_ASSET.Services.NewFolder
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> FindOrCreateUserAsync(AddAssetDto assetDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.name == assetDto.user_name && u.company == assetDto.company && u.department == assetDto.department);

            if (user == null)
            {
                user = new User
                {
                    name = assetDto.user_name,
                    company = assetDto.company,
                    department = assetDto.department,
                    employee_id = assetDto.employee_id
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(assetDto.employee_id) && user.employee_id != assetDto.employee_id)
                {
                    user.employee_id = assetDto.employee_id;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }

            return user;
        }
    }
}
