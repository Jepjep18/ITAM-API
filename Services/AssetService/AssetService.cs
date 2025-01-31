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
            try
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(assetImage.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Invalid file type. Only images are allowed.");
                }

                // Find the asset by ID
                var asset = await _context.Assets.FindAsync(assetId);
                if (asset == null)
                {
                    throw new KeyNotFoundException("Asset not found.");
                }

                // Generate a unique file name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(assetImage.FileName);

                // Define the base directory for asset images
                string baseDirectory = Path.Combine(@"C:\ITAM\assets\asset-images");  // Base path for asset images

                // Create a subdirectory based on asset ID
                string directoryPath = Path.Combine(baseDirectory, assetId.ToString());

                // Ensure the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Combine directory path and the unique file name to get the full file path
                string filePath = Path.Combine(directoryPath, uniqueFileName).Replace("\\", "/");

                // Save the file to the file system
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await assetImage.CopyToAsync(stream);
                }

                // Update the asset image path in the database
                asset.asset_image = filePath;
                _context.Assets.Update(asset);
                await _context.SaveChangesAsync();

                return filePath;  // Return the file path to the caller
            }
            catch (Exception ex)
            {
                // Handle errors
                throw new Exception($"Error uploading asset image: {ex.Message}");
            }
        }


        //for computer upload image endpoint
        public async Task<string> UploadComputerImageAsync(int computerId, IFormFile computerImage)
        {
            try
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(computerImage.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Invalid file type. Only images are allowed.");
                }

                // Find the computer by ID
                var computer = await _context.computers.FindAsync(computerId);
                if (computer == null)
                {
                    throw new KeyNotFoundException("Computer not found.");
                }

                // Generate a unique file name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(computerImage.FileName);

                // Define the base directory for computer images
                string baseDirectory = Path.Combine(@"C:\ITAM\assets\computer-images");  // Base path for computer images

                // Create a subdirectory based on computer ID
                string directoryPath = Path.Combine(baseDirectory, computerId.ToString());

                // Ensure the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Combine directory path and the unique file name to get the full file path
                string filePath = Path.Combine(directoryPath, uniqueFileName).Replace("\\", "/");

                // Save the file to the file system
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await computerImage.CopyToAsync(stream);
                }

                // Update the computer image path in the database
                computer.asset_image = filePath;
                _context.computers.Update(computer);
                await _context.SaveChangesAsync();

                return filePath;  // Return the file path to the caller
            }
            catch (Exception ex)
            {
                // Handle errors
                throw new Exception($"Error uploading computer image: {ex.Message}");
            }
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
                liDescription = "No description available";
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

                _context.computers.Add(computer);
                await _context.SaveChangesAsync();

                // Store the computer components in the computer_components table
                await StoreComputerComponentsAsync(computer, assetDto);

                return computer;
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

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                return asset;
            }
        }

        private async Task StoreComputerComponentsAsync(Computer computer, CreateAssetDto assetDto)
        {
            // Create a list of components based on the computer's properties
            var components = new List<ComputerComponents>();

            // Add RAM as a component if it exists
            if (!string.IsNullOrWhiteSpace(computer.ram))
            {
                components.Add(new ComputerComponents
                {
                    type = "RAM",
                    description = computer.ram,
                    asset_barcode = $"{computer.asset_barcode}",
                    status = "Available",
                    owner_id = null, // Vacant asset, so no owner
                    history = new List<string>(),
                    computer_id = computer.id // Set the computer_id foreign key
                });
            }

            // Add SSD as a component if it exists
            if (!string.IsNullOrWhiteSpace(computer.ssd))
            {
                components.Add(new ComputerComponents
                {
                    type = "SSD",
                    description = computer.ssd,
                    asset_barcode = $"{computer.asset_barcode}",
                    status = "Available",
                    owner_id = null, // Vacant asset, so no owner
                    history = new List<string>(),
                    computer_id = computer.id // Set the computer_id foreign key
                });
            }

            // Add HDD as a component if it exists
            if (!string.IsNullOrWhiteSpace(computer.hdd))
            {
                components.Add(new ComputerComponents
                {
                    type = "HDD",
                    description = computer.hdd,
                    asset_barcode = $"{computer.asset_barcode}",
                    status = "Available",
                    owner_id = null, // Vacant asset, so no owner
                    history = new List<string>(),
                    computer_id = computer.id // Set the computer_id foreign key
                });
            }

            // Add GPU as a component if it exists
            if (!string.IsNullOrWhiteSpace(computer.gpu))
            {
                components.Add(new ComputerComponents
                {
                    type = "GPU",
                    description = computer.gpu,
                    asset_barcode = $"{computer.asset_barcode}",
                    status = "Available",
                    owner_id = null, // Vacant asset, so no owner
                    history = new List<string>(),
                    computer_id = computer.id // Set the computer_id foreign key
                });
            }

            // Add all components to the database
            _context.computer_components.AddRange(components);
            await _context.SaveChangesAsync();
        }


        //for assigning user for vacant-asset items
        public async Task<Asset> AssignOwnerToAssetAsync(AssignOwnerDto assignOwnerDto)
        {
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.id == assignOwnerDto.asset_id && a.owner_id == null);

            if (asset == null)
            {
                throw new KeyNotFoundException("Vacant asset not found or already has an owner.");
            }

            var user = await _context.Users.FindAsync(assignOwnerDto.owner_id);
            if (user == null)
            {
                throw new KeyNotFoundException("Owner not found.");
            }

            asset.owner_id = assignOwnerDto.owner_id;

            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();

            await UpdateUserAccountabilityListAsync(user, asset);

            return asset; 
        }

        private async Task UpdateUserAccountabilityListAsync(User user, Asset asset)
        {
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
            }

            var existingAssetIds = string.IsNullOrEmpty(userAccountabilityList.asset_ids)
                ? new List<int>()
                : userAccountabilityList.asset_ids.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).Select(int.Parse).ToList();

            if (!existingAssetIds.Contains(asset.id))
            {
                existingAssetIds.Add(asset.id);
                userAccountabilityList.asset_ids = string.Join(",", existingAssetIds);
            }

            _context.user_accountability_lists.Update(userAccountabilityList);
            await _context.SaveChangesAsync();
        }


        //for assigning user for vacant-computer items
        public async Task<Computer> AssignOwnerToComputerAsync(AssignOwnerforComputerDto assignOwnerforComputerDto)
        {
            var computer = await _context.computers
                .FirstOrDefaultAsync(c => c.id == assignOwnerforComputerDto.computer_id && c.owner_id == null);

            if (computer == null)
            {
                throw new KeyNotFoundException("Vacant computer not found or already has an owner.");
            }

            var user = await _context.Users.FindAsync(assignOwnerforComputerDto.owner_id);
            if (user == null)
            {
                throw new KeyNotFoundException("Owner not found.");
            }

            computer.owner_id = assignOwnerforComputerDto.owner_id;

            _context.computers.Update(computer);
            await _context.SaveChangesAsync();

            await UpdateUserAccountabilityListAsync(user, computer);

            return computer;
        }

        private async Task UpdateUserAccountabilityListAsync(User user, Computer computer)
        {
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
            }

            var existingComputerIds = string.IsNullOrEmpty(userAccountabilityList.computer_ids)
                ? new List<int>()
                : userAccountabilityList.computer_ids.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).Select(int.Parse).ToList();

            if (!existingComputerIds.Contains(computer.id))
            {
                existingComputerIds.Add(computer.id);
                userAccountabilityList.computer_ids = string.Join(",", existingComputerIds);
            }

            _context.user_accountability_lists.Update(userAccountabilityList);
            await _context.SaveChangesAsync();
        }






        //for get by type endpoint 
        public async Task<PaginatedResponse<Asset>> GetAssetsByTypeAsync(
        string type,
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            // Query for Assets
            var assetQuery = _context.Assets
                .Where(a => a.type.ToLower() == type.ToLower())
                .AsQueryable();

            // Query for Computers
            var computerQuery = _context.computers
                .Where(c => c.type.ToLower() == type.ToLower())
                .AsQueryable();

            // Apply search filter to both queries if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                assetQuery = assetQuery.Where(asset =>
                    asset.asset_barcode.Contains(searchTerm) ||
                    asset.type.Contains(searchTerm) ||
                    asset.brand.Contains(searchTerm));

                computerQuery = computerQuery.Where(computer =>
                    computer.asset_barcode.Contains(searchTerm) ||
                    computer.type.Contains(searchTerm) ||
                    computer.brand.Contains(searchTerm) ||
                    computer.serial_no.Contains(searchTerm) ||
                    computer.model.Contains(searchTerm));
            }

            // Combine the results of both queries into a single list
            var combinedQuery = assetQuery
                .Select(asset => new Asset
                {
                    id = asset.id,
                    type = asset.type,
                    asset_barcode = asset.asset_barcode,
                    brand = asset.brand,
                    // Map other fields as needed
                })
                .Union(computerQuery.Select(computer => new Asset
                {
                    id = computer.id,
                    type = computer.type,
                    asset_barcode = computer.asset_barcode,
                    brand = computer.brand,
                    // Map other fields as needed
                }))
                .AsQueryable();

            // Apply sorting based on the order
            combinedQuery = sortOrder.ToLower() switch
            {
                "desc" => combinedQuery.OrderByDescending(a => a.id),
                "asc" or _ => combinedQuery.OrderBy(a => a.id),
            };

            // Get the total count of the filtered and sorted assets
            var totalItems = await combinedQuery.CountAsync();

            // Apply pagination (skip and take)
            var paginatedData = await combinedQuery
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
                    EF.Functions.Like(asset.asset_barcode, $"%{searchTerm}%") ||
                    EF.Functions.Like(asset.type, $"%{searchTerm}%") ||  // Added "type" column
                    EF.Functions.Like(asset.brand, $"%{searchTerm}%"));
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


        //for get by id assets endpoint
        public async Task<Asset?> GetAssetByIdAsync(int id)
        {
            try
            {
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.id == id); 

                return asset;
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                throw new Exception($"Error retrieving asset with ID {id}: {ex.Message}");
            }
        }

        public async Task<Asset> UpdateAssetAsync(int assetId, UpdateAssetDto assetDto, int ownerId)
        {
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.id == assetId);

                if (asset == null)
                {
                    return null; // Asset not found
                }

                // Check if the owner is changing
                if (asset.owner_id != ownerId)
                {
                    if (asset.history == null)
                    {
                        asset.history = new List<string>();
                    }

                    // Only add to history if there was a previous owner
                    if (asset.owner_id.HasValue) // Check if owner_id is not null
                    {
                        var previousOwnerName = await _context.Users
                            .Where(u => u.id == asset.owner_id)
                            .Select(u => u.name)
                            .FirstOrDefaultAsync();

                        string previousOwner = previousOwnerName ?? "Unknown";
                        asset.history.Add(previousOwner);

                        // Remove the asset from the previous owner's accountability list
                        var previousOwnerAccountability = await _context.user_accountability_lists
                            .FirstOrDefaultAsync(al => al.owner_id == asset.owner_id);

                        if (previousOwnerAccountability != null)
                        {
                            // Remove the asset ID from the previous owner's asset_ids
                            var assetIds = previousOwnerAccountability.asset_ids?
                                .Split(',')
                                .Where(id => id != assetId.ToString())
                                .ToList();

                            previousOwnerAccountability.asset_ids = assetIds != null && assetIds.Any()
                                ? string.Join(",", assetIds)
                                : null;

                            // Remove the asset from the previous owner's assets list
                            previousOwnerAccountability.assets?.Remove(asset);

                            // If the previous owner's accountability list is empty, remove it
                            if (string.IsNullOrWhiteSpace(previousOwnerAccountability.asset_ids))
                            {
                                _context.user_accountability_lists.Remove(previousOwnerAccountability);
                            }
                            else
                            {
                                _context.user_accountability_lists.Update(previousOwnerAccountability);
                            }
                        }
                    }

                    // Check if the new owner already has an accountability list
                    var newOwnerAccountability = await _context.user_accountability_lists
                        .FirstOrDefaultAsync(al => al.owner_id == ownerId);

                    if (newOwnerAccountability == null)
                    {
                        // Fetch the last accountability_code and tracking_code
                        var lastAccountability = await _context.user_accountability_lists
                            .OrderByDescending(al => al.id)
                            .FirstOrDefaultAsync();

                        int lastAccountabilityNumber = 0;
                        int lastTrackingNumber = 0;

                        if (lastAccountability != null)
                        {
                            // Extract the last numbers from accountability_code and tracking_code
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

                        // Increment the numbers
                        int newAccountabilityNumber = lastAccountabilityNumber + 1;
                        int newTrackingNumber = lastTrackingNumber + 1;

                        // Generate new accountability_code and tracking_code
                        string newAccountabilityCode = $"ACID-{newAccountabilityNumber:D4}";
                        string newTrackingCode = $"TRID-{newTrackingNumber:D4}";

                        var newAccountabilityList = new UserAccountabilityList
                        {
                            owner_id = ownerId,
                            accountability_code = newAccountabilityCode,
                            tracking_code = newTrackingCode,
                            asset_ids = asset.id.ToString(),
                            assets = new List<Asset> { asset }
                        };

                        await _context.user_accountability_lists.AddAsync(newAccountabilityList);
                    }
                    else
                    {
                        // Ensure asset_ids is not null or empty
                        newOwnerAccountability.asset_ids = string.IsNullOrWhiteSpace(newOwnerAccountability.asset_ids)
                            ? asset.id.ToString()
                            : $"{newOwnerAccountability.asset_ids},{asset.id}";

                        newOwnerAccountability.assets ??= new List<Asset>();
                        newOwnerAccountability.assets.Add(asset);

                        _context.user_accountability_lists.Update(newOwnerAccountability);
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

                return asset;
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


        //for get asset filename endpoint
        public async Task<string> GetAssetImageByFilenameAsync(string filename)
        {
            try
            {
                // Define the base directory for asset images
                string baseDirectory = @"C:\ITAM\assets\asset-images";

                // Get all subdirectories in the base directory
                var directories = Directory.GetDirectories(baseDirectory);
                string filePath = null;

                // Search for the file in all subdirectories
                foreach (var directory in directories)
                {
                    var potentialPath = Path.Combine(directory, filename);
                    if (File.Exists(potentialPath))
                    {
                        filePath = potentialPath;
                        break;
                    }
                }

                // If file is not found in any subdirectory
                if (filePath == null)
                {
                    Console.WriteLine($"File not found. Searched in all subdirectories of: {baseDirectory}");
                    Console.WriteLine($"Filename searched for: {filename}");
                    Console.WriteLine($"Available directories: {string.Join(", ", directories)}");

                    throw new FileNotFoundException($"Asset image '{filename}' not found in any asset directory");
                }

                return filePath;
            }
            catch (Exception ex)
            {
                // Add more context to the error
                Console.WriteLine($"Error details: {ex}");
                throw new Exception($"Error fetching asset image: {ex.Message}", ex);
            }
        }






    }
}
