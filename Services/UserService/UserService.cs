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

        //for updating asset endpoint or creating new user for not existing user
        public async Task<int> GetOrCreateUserAsync(UpdateAssetDto assetDto)
        {
            // Check if the user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.name == assetDto.user_name
                                       && u.company == assetDto.company
                                       && u.department == assetDto.department
                                       && u.employee_id == assetDto.employee_id);

            if (existingUser != null)
            {
                return existingUser.id; // Return the existing user's id
            }
            else
            {
                // Create a new user if not found
                var newUser = new User
                {
                    name = assetDto.user_name,
                    company = assetDto.company,
                    department = assetDto.department,
                    employee_id = assetDto.employee_id
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return newUser.id; // Return the new user's id
            }
        }
    }
}
