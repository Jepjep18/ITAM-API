using IT_ASSET.DTOs;
using IT_ASSET.Models;
using IT_ASSET.Models.Logs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace IT_ASSET.Services.ComputerService
{
    public class ComputerService
    {
        private readonly AppDbContext _context;

        public ComputerService(AppDbContext context)
        {
            _context = context;
        }

        //for get all computer items endpoint
        public async Task<PaginatedResponse<Computer>> GetAllComputersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            var query = _context.computers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(computer =>
                    EF.Functions.Like(computer.serial_no, $"%{searchTerm}%") ||
                    EF.Functions.Like(computer.model, $"%{searchTerm}%") ||
                    EF.Functions.Like(computer.brand, $"%{searchTerm}%") ||
                    EF.Functions.Like(computer.type, $"%{searchTerm}%")); 
            }

            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(computer => computer.id),
                "asc" or _ => query.OrderBy(computer => computer.id),
            };

            var totalItems = await query.CountAsync();

            var paginatedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResponse<Computer>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }


        //for update computer data endpoint
        public async Task<Computer> UpdateComputerAsync(int computerId, UpdateComputerDto computerDto, int ownerId, ClaimsPrincipal user)
        {
            try
            {
                var computer = await _context.computers.FirstOrDefaultAsync(c => c.id == computerId);

                if (computer == null)
                {
                    return null;
                }

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore 
                };

                string originalData = JsonConvert.SerializeObject(computer, settings); 

                if (computer.owner_id != ownerId)
                {
                    if (computer.history == null)
                    {
                        computer.history = new List<string>();
                    }

                    if (computer.owner_id.HasValue) 
                    {
                        var previousOwnerName = await _context.Users
                            .Where(u => u.id == computer.owner_id)
                            .Select(u => u.name)
                            .FirstOrDefaultAsync();

                        string previousOwner = previousOwnerName ?? "Unknown";
                        computer.history.Add(previousOwner);

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
                computer.owner_id = ownerId;

                _context.computers.Update(computer);

                await UpdateComputerComponentsAsync(computer, ownerId);

                string updatedData = JsonConvert.SerializeObject(computer, settings); 

                var performedByUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM_USER";

                var computerLog = new Computer_logs
                {
                    computer_id = computer.id,
                    action = "UPDATE",
                    performed_by_user_id = performedByUserId,
                    timestamp = DateTime.UtcNow,
                    details = $"Computer ID {computer.id} was updated by User ID {performedByUserId}. " +
                              $"Original Data: {originalData}, Updated Data: {updatedData}"
                };

                _context.computer_Logs.Add(computerLog);

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
                if (component.owner_id != ownerId)
                {
                    component.owner_id = ownerId;
                }

                if (component.owner_id != null)
                {
                    component.status = "Released"; 
                }

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

                _context.computer_components.Update(component);
            }
        }




        //for get computers by id endpoints
        public async Task<object> GetComputerByIdAsync(int id)
        {
            try
            {
                var computer = await _context.computers
                    .Where(c => c.id == id)
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
                        c.date_modified,
                        owner = c.owner_id != null ? new
                        {
                            id = c.owner_id,
                            name = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.name).FirstOrDefault(),
                            company = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.company).FirstOrDefault(),
                            department = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.department).FirstOrDefault(),
                            employee_id = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.employee_id).FirstOrDefault()
                        } : null
                    })
                    .FirstOrDefaultAsync();

                return computer;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving computer with ID {id}: {ex.Message}");
            }
        }





        //for get computer filename endpoint
        public async Task<string> GetComputerImageByFilenameAsync(string filename)
        {
            try
            {
                string baseDirectory = @"C:\ITAM\assets\computer-images";

                var directories = Directory.GetDirectories(baseDirectory);
                string filePath = null;

                var rootPath = Path.Combine(baseDirectory, filename);
                if (File.Exists(rootPath))
                {
                    filePath = rootPath;
                }
                else
                {
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
                Console.WriteLine($"Error details: {ex}");
                throw new Exception($"Error fetching computer image: {ex.Message}", ex);
            }
        }


        //for delete endpoint 
        public async Task<ServiceResponse> DeleteComputerAsync(int id, ClaimsPrincipal user)
        {
            var computer = await _context.computers.FirstOrDefaultAsync(c => c.id == id);

            if (computer == null)
            {
                return new ServiceResponse { Success = false, StatusCode = 404, Message = "Computer not found." };
            }

            if (computer.is_deleted)
            {
                return new ServiceResponse { Success = false, StatusCode = 409, Message = "Computer is already deleted." };
            }

            var assignedUser = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ua => ua.computer_ids != null && ua.computer_ids.Contains(id.ToString()));

            if (assignedUser != null)
            {
                var updatedComputerIds = assignedUser.computer_ids
                    .Split(',')
                    .Where(cid => cid.Trim() != id.ToString()) 
                    .ToArray();

                assignedUser.computer_ids = updatedComputerIds.Length > 0
                    ? string.Join(",", updatedComputerIds)
                    : null; 

                _context.user_accountability_lists.Update(assignedUser);
            }

            computer.is_deleted = true;
            computer.date_modified = DateTime.UtcNow;

            _context.computers.Update(computer);

            string performedByUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System"; 

            var computerLog = new Computer_logs
            {
                computer_id = computer.id,
                action = "DELETE",
                performed_by_user_id = performedByUserId,
                timestamp = DateTime.UtcNow,
                details = $"Computer ID {computer.id} was deleted by User ID {performedByUserId}."
            };

            _context.computer_Logs.Add(computerLog);

            await _context.SaveChangesAsync();

            return new ServiceResponse { Success = true, StatusCode = 200, Message = "Computer deleted successfully." };
        }






    }
}
