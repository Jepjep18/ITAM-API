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


        public async Task<Computer> UpdateComputerAsync(int computerId, UpdateComputerDto computerDto, int ownerId)
        {
            try
            {
                var computer = await _context.computers.FirstOrDefaultAsync(c => c.id == computerId);

                if (computer == null)
                {
                    return null; // Computer not found
                }

                // Maintain history if the owner is changing
                if (computer.owner_id != ownerId)
                {
                    if (computer.history == null)
                    {
                        computer.history = new List<string>();
                    }

                    // Only add to history if there was a previous owner
                    if (computer.owner_id.HasValue) // Check if owner_id is not null
                    {
                        var previousOwnerName = await _context.Users
                            .Where(u => u.id == computer.owner_id)
                            .Select(u => u.name)
                            .FirstOrDefaultAsync();

                        string previousOwner = previousOwnerName ?? "Unknown";
                        computer.history.Add(previousOwner);

                        // Remove the computer from the previous owner's accountability list
                        var previousOwnerAccountability = await _context.user_accountability_lists
                            .FirstOrDefaultAsync(al => al.owner_id == computer.owner_id);

                        if (previousOwnerAccountability != null)
                        {
                            var computerIds = previousOwnerAccountability.computer_ids?
                                .Split(',')
                                .Where(id => id != computerId.ToString())
                                .ToList();

                            previousOwnerAccountability.computer_ids = computerIds != null && computerIds.Any()
                                ? string.Join(",", computerIds)
                                : null;

                            if (string.IsNullOrWhiteSpace(previousOwnerAccountability.asset_ids) &&
                                string.IsNullOrWhiteSpace(previousOwnerAccountability.computer_ids))
                            {
                                _context.user_accountability_lists.Remove(previousOwnerAccountability);
                            }
                            else
                            {
                                _context.user_accountability_lists.Update(previousOwnerAccountability);
                            }
                        }
                    }

                    var newOwnerAccountability = await _context.user_accountability_lists
                        .FirstOrDefaultAsync(al => al.owner_id == ownerId);

                    if (newOwnerAccountability == null)
                    {
                        var lastAccountability = await _context.user_accountability_lists
                            .OrderByDescending(al => al.id)
                            .FirstOrDefaultAsync();

                        int lastAccountabilityNumber = 0;
                        int lastTrackingNumber = 0;

                        if (lastAccountability != null)
                        {
                            var lastAccountabilityCode = lastAccountability.accountability_code;
                            var lastTrackingCode = lastAccountability.tracking_code;

                            if (lastAccountabilityCode != null && lastAccountabilityCode.StartsWith("ACID-"))
                            {
                                lastAccountabilityNumber = int.Parse(lastAccountabilityCode.Substring(5));
                            }

                            if (lastTrackingCode != null && lastTrackingCode.StartsWith("TRID-"))
                            {
                                lastTrackingNumber = int.Parse(lastTrackingCode.Substring(5));
                            }
                        }

                        int newAccountabilityNumber = lastAccountabilityNumber + 1;
                        int newTrackingNumber = lastTrackingNumber + 1;

                        string newAccountabilityCode = $"ACID-{newAccountabilityNumber:D4}";
                        string newTrackingCode = $"TRID-{newTrackingNumber:D4}";

                        var newAccountabilityList = new UserAccountabilityList
                        {
                            owner_id = ownerId,
                            accountability_code = newAccountabilityCode,
                            tracking_code = newTrackingCode,
                            computer_ids = computer.id.ToString(),
                        };

                        await _context.user_accountability_lists.AddAsync(newAccountabilityList);
                    }
                    else
                    {
                        newOwnerAccountability.computer_ids = string.IsNullOrWhiteSpace(newOwnerAccountability.computer_ids)
                            ? computer.id.ToString()
                            : $"{newOwnerAccountability.computer_ids},{computer.id}";

                        _context.user_accountability_lists.Update(newOwnerAccountability);
                    }
                }

                // Update the computer properties
                computer.type = computerDto.type;
                computer.date_acquired = computerDto.date_acquired;
                computer.asset_barcode = computerDto.asset_barcode;
                computer.brand = computerDto.brand;
                computer.model = computerDto.model;
                computer.ram = computerDto.ram;
                computer.ssd = computerDto.ssd;
                computer.hdd = computerDto.hdd;
                computer.gpu = computerDto.gpu;
                computer.size = computerDto.size;
                computer.color = computerDto.color;
                computer.serial_no = computerDto.serial_no;
                computer.po = computerDto.po;
                computer.warranty = computerDto.warranty;
                computer.cost = computerDto.cost;
                computer.remarks = computerDto.remarks;
                computer.owner_id = ownerId; // Update owner_id

                _context.computers.Update(computer);

                // Update the ComputerComponents table for the relevant components
                await UpdateComputerComponentsAsync(computer, ownerId);

                await _context.SaveChangesAsync();

                return computer;
            }
            catch (DbUpdateException dbEx)
            {
                throw new Exception($"Database error: {dbEx.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error: {ex.Message}");
            }
        }

        private async Task UpdateComputerComponentsAsync(Computer computer, int ownerId)
        {
            var components = await _context.computer_components
                .Where(c => c.computer_id == computer.id)
                .ToListAsync();

            foreach (var component in components)
            {
                // If the component's owner_id is different, update it
                if (component.owner_id != ownerId)
                {
                    component.owner_id = ownerId;
                }

                // Update the component's status if owner_id is not null (i.e., assigned)
                if (component.owner_id != null)
                {
                    component.status = "Released"; // Update the status to 'Released' when an owner is assigned
                }

                // Update component details if necessary (e.g., RAM, SSD, HDD, GPU)
                if (component.type == "RAM" && computer.ram != component.description)
                {
                    component.description = computer.ram;
                }
                else if (component.type == "SSD" && computer.ssd != component.description)
                {
                    component.description = computer.ssd;
                }
                else if (component.type == "HDD" && computer.hdd != component.description)
                {
                    component.description = computer.hdd;
                }
                else if (component.type == "GPU" && computer.gpu != component.description)
                {
                    component.description = computer.gpu;
                }

                // Update the component in the database
                _context.computer_components.Update(component);
            }
        }

        //for get computers by id endpoints
        public async Task<Computer?> GetComputerByIdAsync(int id)
        {
            try
            {
                // Query the database to find the computer by ID
                var computer = await _context.computers
                    .FirstOrDefaultAsync(c => c.id == id); // Assuming 'Computers' is your DbSet

                return computer;
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                throw new Exception($"Error retrieving computer with ID {id}: {ex.Message}");
            }
        }


        //for get computer filename endpoint
        public async Task<string> GetComputerImageByFilenameAsync(string filename)
        {
            try
            {
                // Define the base directory for computer images
                string baseDirectory = @"C:\ITAM\assets\computer-images";

                // Get all subdirectories in the base directory
                var directories = Directory.GetDirectories(baseDirectory);
                string filePath = null;

                // First try the root directory
                var rootPath = Path.Combine(baseDirectory, filename);
                if (File.Exists(rootPath))
                {
                    filePath = rootPath;
                }
                else
                {
                    // If not in root, search through all subdirectories
                    foreach (var directory in directories)
                    {
                        var potentialPath = Path.Combine(directory, filename);
                        if (File.Exists(potentialPath))
                        {
                            filePath = potentialPath;
                            break;
                        }
                    }
                }

                // If file is not found anywhere
                if (filePath == null)
                {
                    Console.WriteLine($"File not found. Searched in base directory and all subdirectories of: {baseDirectory}");
                    Console.WriteLine($"Filename searched for: {filename}");
                    Console.WriteLine($"Available directories: {string.Join(", ", directories)}");

                    throw new FileNotFoundException($"Computer image '{filename}' not found in any directory");
                }

                return filePath;
            }
            catch (Exception ex)
            {
                // Add more context to the error
                Console.WriteLine($"Error details: {ex}");
                throw new Exception($"Error fetching computer image: {ex.Message}", ex);
            }
        }



    }
}
