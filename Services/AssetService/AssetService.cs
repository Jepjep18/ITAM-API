using IT_ASSET.DTOs;
using IT_ASSET.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT_ASSET.Services.NewFolder
{
    public class AssetService
    {
        private readonly AppDbContext _context;
        private readonly UserService _userService;

        public AssetService(AppDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<string> AddAssetAsync(AddAssetDto assetDto)
        {
            
            if (string.IsNullOrWhiteSpace(assetDto.type) || string.IsNullOrWhiteSpace(assetDto.asset_barcode))
            {
                throw new ArgumentException("Type and Asset Barcode are required.");
            }

            var user = await _userService.FindOrCreateUserAsync(assetDto);
            if (user == null) throw new Exception("User not found or could not be created.");

            var liDescription = GenerateLiDescription(assetDto);

            var computerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPU", "CPU CORE i7 10th GEN", "CPU INTEL CORE i5", "Laptop", "Laptop Macbook AIR, NB 15S-DUI537TU"
            };

            try
            {
                if (computerTypes.Contains(assetDto.type))
                {
                    var computer = new Computer
                    {
                        type = assetDto.type,
                        asset_barcode = assetDto.asset_barcode,
                        owner_id = user.id,
                        
                    };

                    _context.computers.Add(computer);
                    await _context.SaveChangesAsync();

                    
                    await HandleUserAccountabilityListAsync(user, computer);

                    
                    await StoreInComputerComponentsAsync(assetDto, user);

                    return "Computer added successfully.";
                }
                else
                {
                    var asset = new Asset
                    {
                        type = assetDto.type,
                        asset_barcode = assetDto.asset_barcode,
                        owner_id = user.id,
                        
                    };

                    _context.Assets.Add(asset);
                    await _context.SaveChangesAsync();

                    
                    await HandleUserAccountabilityListAsync(user, asset);

                    return "Asset added successfully.";
                }
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database Update Exception: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }



        public async Task StoreInComputerComponentsAsync(AddAssetDto assetDto, User user)
        {
            
            var assetBarcode = assetDto.asset_barcode; 
            var ownerId = user.id;

            // Define headers that you want to store as 'type'
            var headers = new string[] { "RAM", "SSD", "HDD", "GPU" };
            var values = new string[]
            {
                assetDto.ram,  
                assetDto.ssd,  
                assetDto.hdd,  
                assetDto.gpu   
            };

            
            for (int i = 0; i < headers.Length; i++)
            {
                var description = values[i];

                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    var component = new ComputerComponents
                    {
                        type = headers[i],
                        description = description,
                        asset_barcode = assetBarcode,
                        status = ownerId != null ? "Released" : "New",
                        history = new List<string>(), 
                        owner_id = ownerId
                    };

                    _context.computer_components.Add(component);
                    Console.WriteLine($"Adding {component.type}: {component.description}");

                    
                    await HandleUserAccountabilityListAsync(user, component);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("Data saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving: {ex.Message}");
            }
        }


        private string GenerateLiDescription(AddAssetDto assetDto)
        {
            var liDescription = string.Join(" ",
                assetDto.brand?.Trim(),
                assetDto.type?.Trim(),
                assetDto.model?.Trim(),
                assetDto.ram?.Trim(),
                assetDto.ssd?.Trim(),
                assetDto.hdd?.Trim(),
                assetDto.gpu?.Trim(),
                assetDto.size?.Trim(),
                assetDto.color?.Trim()).Trim();

            return string.IsNullOrWhiteSpace(liDescription) ? "No description available" : liDescription;
        }

        private async Task HandleUserAccountabilityListAsync(User user, object item)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User cannot be null.");
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Item cannot be null.");
            }

            var userAccountabilityList = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ual => ual.owner_id == user.id);

            if (userAccountabilityList == null)
            {
                
                var accountabilityCode = GenerateAccountabilityCode();
                var trackingCode = GenerateTrackingCode();

                userAccountabilityList = new UserAccountabilityList
                {
                    accountability_code = accountabilityCode,
                    tracking_code = trackingCode,
                    owner_id = user.id,
                    asset_ids = string.Empty,   
                    computer_ids = string.Empty 
                };

                
                _context.user_accountability_lists.Add(userAccountabilityList);
                await _context.SaveChangesAsync();  
                Console.WriteLine($"Created new UserAccountabilityList for User ID: {user.id}");
            }
            else
            {
                Console.WriteLine($"UserAccountabilityList found for User ID: {user.id}");
            }

            
            try
            {
                if (item is Asset asset)
                {
                    Console.WriteLine($"Adding Asset ID: {asset.id} to User {user.id}");

                    // If asset_ids is null or empty, initialize as an empty list
                    var existingAssetIds = string.IsNullOrEmpty(userAccountabilityList.asset_ids)
                        ? new List<int>()
                        : userAccountabilityList.asset_ids.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).Select(int.Parse).ToList();

                    existingAssetIds.Add(asset.id);
                    userAccountabilityList.asset_ids = string.Join(",", existingAssetIds);
                }
                else if (item is Computer computer) // Added this case to handle Computer objects
                {
                    Console.WriteLine($"Adding Computer ID: {computer.id} to User {user.id}");

                    // If computer_ids is null or empty, initialize as an empty list
                    var existingComponentIds = string.IsNullOrEmpty(userAccountabilityList.computer_ids)
                        ? new List<int>()
                        : userAccountabilityList.computer_ids.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).Select(int.Parse).ToList();

                    
                    if (!existingComponentIds.Contains(computer.id))
                    {
                        existingComponentIds.Add(computer.id);
                    }

                    userAccountabilityList.computer_ids = string.Join(",", existingComponentIds);
                }

                
                Console.WriteLine($"Updated asset_ids: {userAccountabilityList.asset_ids}");
                Console.WriteLine($"Updated computer_ids: {userAccountabilityList.computer_ids}");

                
                _context.user_accountability_lists.Update(userAccountabilityList);
                await _context.SaveChangesAsync();
                Console.WriteLine("User Accountability List updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while updating User Accountability List: {ex.Message}");
                throw;
            }
        }

        private string GenerateAccountabilityCode()
        {
            var lastAccountabilityCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.accountability_code)
                .Select(ual => ual.accountability_code)
                .FirstOrDefault();

            int nextCode = 1;

            if (!string.IsNullOrEmpty(lastAccountabilityCode))
            {
                var lastNumber = lastAccountabilityCode.Substring(lastAccountabilityCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out int lastNumberParsed))
                {
                    nextCode = lastNumberParsed + 1;
                }
            }

            return $"ACID-{nextCode:D4}";
        }

        private string GenerateTrackingCode()
        {
            var lastTrackingCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.tracking_code)
                .Select(ual => ual.tracking_code)
                .FirstOrDefault();

            int nextCode = 1;

            if (!string.IsNullOrEmpty(lastTrackingCode))
            {
                var lastNumber = lastTrackingCode.Substring(lastTrackingCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out int lastNumberParsed))
                {
                    nextCode = lastNumberParsed + 1;
                }
            }

            return $"TRID-{nextCode:D4}";
        }


        //for asset upload image endpoint 
        public async Task<string> UploadAssetImageAsync(int assetId, IFormFile assetImage)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(assetImage.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Only images are allowed.");
            }

            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null)
            {
                throw new KeyNotFoundException("Asset not found.");
            }

            var fileName = $"{assetId}_{Path.GetFileName(assetImage.FileName)}";
            var directoryPath = @"C:\Users\JBARNADO\Desktop\ITAM\asset_image";
            var filePath = Path.Combine(directoryPath, fileName);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await assetImage.CopyToAsync(stream);
            }

            asset.asset_image = filePath;
            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();

            return filePath;
        }

        //for computer upload image endpoint
        public async Task<string> UploadComputerImageAsync(int computerId, IFormFile computerImage)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(computerImage.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Only images are allowed.");
            }

            var computer = await _context.computers.FindAsync(computerId);
            if (computer == null)
            {
                throw new KeyNotFoundException("Computer not found.");
            }

            var fileName = $"{computerId}_{Path.GetFileName(computerImage.FileName)}";
            var directoryPath = @"C:\Users\JBARNADO\Desktop\ITAM\computer_image";  
            var filePath = Path.Combine(directoryPath, fileName);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await computerImage.CopyToAsync(stream);
            }

            computer.asset_image = filePath; 
            _context.computers.Update(computer);
            await _context.SaveChangesAsync();

            return filePath;  
        }


        //for create-vacant-asset/computer endpoint 
        public async Task<object> CreateVacantAssetAsync(CreateAssetDto assetDto)
        {
            // Define types that should go to the computer database
            var computerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPU",
                "CPU CORE i7 10th GEN",
                "CPU INTEL CORE i5",
                "Laptop",
                "Laptop Macbook AIR, NB 15S-DUI537TU"
            };

            // Validate and prepare the li_description
            var liDescription = string.Join(" ",
                assetDto.brand?.Trim(),
                assetDto.type?.Trim(),
                assetDto.model?.Trim(),
                assetDto.ram?.Trim(),
                assetDto.ssd?.Trim(),
                assetDto.hdd?.Trim(),
                assetDto.gpu?.Trim(),
                assetDto.size?.Trim(),
                assetDto.color?.Trim()).Trim();

            if (string.IsNullOrWhiteSpace(liDescription))
            {
                liDescription = "No description available"; // Default value
            }

            // Create a new asset or computer depending on the type
            if (computerTypes.Contains(assetDto.type))
            {
                // Create a new Computer object if type matches one of the computer-related types
                var computer = new Computer
                {
                    type = assetDto.type,
                    asset_barcode = assetDto.asset_barcode,
                    brand = assetDto.brand,
                    model = assetDto.model,
                    ram = assetDto.ram,
                    ssd = assetDto.ssd,
                    hdd = assetDto.hdd,
                    gpu = assetDto.gpu,
                    size = assetDto.size,
                    color = assetDto.color,
                    serial_no = assetDto.serial_no,
                    po = assetDto.po,
                    warranty = assetDto.warranty,
                    cost = assetDto.cost,
                    remarks = assetDto.remarks,
                    li_description = liDescription,
                    date_acquired = assetDto.date_acquired,
                    asset_image = assetDto.asset_image,
                    owner_id = null,
                    history = new List<string>(),
                    date_created = DateTime.UtcNow
                };

                // Add the computer to the database
                _context.computers.Add(computer);
                await _context.SaveChangesAsync();

                return computer; // Return the created computer object
            }
            else
            {
                // Create a new Asset object for other types
                var asset = new Asset
                {
                    type = assetDto.type,
                    asset_barcode = assetDto.asset_barcode,
                    brand = assetDto.brand,
                    model = assetDto.model,
                    ram = assetDto.ram,
                    ssd = assetDto.ssd,
                    hdd = assetDto.hdd,
                    gpu = assetDto.gpu,
                    size = assetDto.size,
                    color = assetDto.color,
                    serial_no = assetDto.serial_no,
                    po = assetDto.po,
                    warranty = assetDto.warranty,
                    cost = assetDto.cost,
                    remarks = assetDto.remarks,
                    li_description = liDescription,
                    date_acquired = assetDto.date_acquired,
                    asset_image = assetDto.asset_image,
                    owner_id = null,
                    history = new List<string>(),
                    date_created = DateTime.UtcNow
                };

                // Add the asset to the database
                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                return asset; // Return the created asset object
            }
        }



        //for get by type endpoint 
        public async Task<PaginatedResponse<Asset>> GetAssetsByTypeAsync(
            string type,
            int pageNumber = 1,
            int pageSize = 10,
            string sortOrder = "asc",
            string? searchTerm = null)
        {
            var query = _context.Assets
                .Where(a => a.type.ToLower() == type.ToLower())
                .AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(asset =>
                    asset.asset_barcode.Contains(searchTerm) ||
                    asset.type.Contains(searchTerm) ||
                    asset.brand.Contains(searchTerm));
            }

            // Apply sorting based on the order
            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(asset => asset.id),
                "asc" or _ => query.OrderBy(asset => asset.id),
            };

            // Get the total count of the filtered and sorted assets
            var totalItems = await query.CountAsync();

            // Apply pagination (skip and take)
            var paginatedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Return the paginated response
            return new PaginatedResponse<Asset>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        //for get all items endpoint 
        public async Task<PaginatedResponse<Asset>> GetAllAssetsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string sortOrder = "asc",
            string? searchTerm = null)
        {
            var query = _context.Assets.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(asset =>
                    asset.asset_barcode.Contains(searchTerm) ||
                    asset.type.Contains(searchTerm) ||
                    asset.brand.Contains(searchTerm));
            }

            // Apply sorting based on the order
            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(asset => asset.id),
                "asc" or _ => query.OrderBy(asset => asset.id),
            };

            // Get the total count of the filtered and sorted assets
            var totalItems = await query.CountAsync();

            // Apply pagination (skip and take)
            var paginatedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Return the paginated response
            return new PaginatedResponse<Asset>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        //for update endpoint 
        public async Task<Asset> UpdateAssetAsync(int assetId, UpdateAssetDto assetDto, int ownerId)
        {
            // Fetch the asset from the database
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.id == assetId);

            if (asset == null)
            {
                return null; // Asset not found
            }

            // Maintain history if the owner is changing
            if (asset.owner_id != ownerId)
            {
                // Ensure that the history list is initialized
                if (asset.history == null)
                {
                    asset.history = new List<string>();
                }

                // Fetch previous owner details
                var previousOwnerName = await _context.Users
                    .Where(u => u.id == asset.owner_id)
                    .Select(u => u.name)
                    .FirstOrDefaultAsync();

                string previousOwner = previousOwnerName ?? "Unknown";
                string newHistoryEntry = $"{previousOwner}";

                // Add new entry to history
                asset.history.Add(newHistoryEntry);

                // Ensure accountability list is initialized for the new owner
                var ownerAccountability = await _context.user_accountability_lists
                    .FirstOrDefaultAsync(al => al.owner_id == ownerId);

                if (ownerAccountability == null)
                {
                    // Create new accountability list if not exists
                    var newAccountabilityList = new UserAccountabilityList
                    {
                        owner_id = ownerId,
                        asset_ids = asset.id.ToString(), // Ensure it's in a format that you need (string)
                        assets = new List<Asset> { asset } // Add asset to the list
                    };

                    _context.user_accountability_lists.Add(newAccountabilityList);
                }
                else
                {
                    // Ensure asset_ids is not null or empty
                    if (string.IsNullOrWhiteSpace(ownerAccountability.asset_ids))
                    {
                        ownerAccountability.asset_ids = asset.id.ToString(); // Initialize if empty
                    }
                    else
                    {
                        ownerAccountability.asset_ids += "," + asset.id.ToString(); // Add asset ID as comma-separated value
                    }

                    // Ensure assets collection is initialized
                    if (ownerAccountability.assets == null)
                    {
                        ownerAccountability.assets = new List<Asset>();
                    }

                    // Add asset to the existing user's accountability list
                    ownerAccountability.assets.Add(asset);

                    _context.user_accountability_lists.Update(ownerAccountability);
                }
            }

            // Update asset details
            asset.li_description = string.Join(" ",
                assetDto.brand?.Trim(),
                assetDto.type?.Trim(),
                assetDto.model?.Trim(),
                assetDto.ram?.Trim(),
                assetDto.ssd?.Trim(),
                assetDto.hdd?.Trim(),
                assetDto.gpu?.Trim(),
                assetDto.size?.Trim(),
                assetDto.color?.Trim()).Trim();

            asset.type = assetDto.type;
            asset.date_acquired = assetDto.date_acquired;
            asset.asset_barcode = assetDto.asset_barcode;
            asset.brand = assetDto.brand;
            asset.model = assetDto.model;
            asset.ram = assetDto.ram;
            asset.ssd = assetDto.ssd;
            asset.hdd = assetDto.hdd;
            asset.gpu = assetDto.gpu;
            asset.size = assetDto.size;
            asset.color = assetDto.color;
            asset.serial_no = assetDto.serial_no;
            asset.po = assetDto.po;
            asset.warranty = assetDto.warranty;
            asset.cost = assetDto.cost;
            asset.remarks = assetDto.remarks;
            asset.owner_id = ownerId;

            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();

            return asset; // Return updated asset
        }


    }
}
