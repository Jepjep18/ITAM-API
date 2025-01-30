using IT_ASSET.DTOs;
using IT_ASSET.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_ASSET.Services.ComputerService
{
    public class ComputerService
    {
        private readonly AppDbContext _context;

        public ComputerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResponse<Computer>> GetAllComputersAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string sortOrder = "asc",
            string? searchTerm = null)
        {
            var query = _context.computers.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(computer =>
                    computer.serial_no.Contains(searchTerm) ||
                    computer.model.Contains(searchTerm) ||
                    computer.brand.Contains(searchTerm));
            }

            // Apply sorting based on the order
            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(computer => computer.id),
                "asc" or _ => query.OrderBy(computer => computer.id),
            };

            // Get the total count of the filtered and sorted computers
            var totalItems = await query.CountAsync();

            // Apply pagination (skip and take)
            var paginatedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Return the paginated response
            return new PaginatedResponse<Computer>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
