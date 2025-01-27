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
            // Find or create the user
            var user = await _userService.FindOrCreateUserAsync(assetDto);
            if (user == null) throw new Exception("Error creating or finding user.");

            // Generate li_description
            var liDescription = GenerateLiDescription(assetDto);

            // Create the asset
            var asset = new Asset
            {
                type = assetDto.type,
                date_acquired = assetDto.date_acquired,
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
                owner_id = user.id,
                li_description = liDescription,
                date_created = DateTime.UtcNow
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            // Handle user accountability list
            await HandleUserAccountabilityListAsync(user, asset);

            return "Asset added successfully.";
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

        private async Task HandleUserAccountabilityListAsync(User user, Asset asset)
        {
            var userAccountabilityList = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ual => ual.owner_id == user.id);

            if (userAccountabilityList == null)
            {
                // Create new accountability and tracking codes
                var accountabilityCode = GenerateAccountabilityCode();
                var trackingCode = GenerateTrackingCode();

                userAccountabilityList = new UserAccountabilityList
                {
                    accountability_code = accountabilityCode,
                    tracking_code = trackingCode,
                    owner_id = user.id,
                    asset_ids = asset.id.ToString()
                };

                _context.user_accountability_lists.Add(userAccountabilityList);
            }
            else
            {
                // Update existing accountability list with new asset ID
                var existingAssetIds = userAccountabilityList.asset_ids.Split(',').Select(int.Parse).ToList();
                existingAssetIds.Add(asset.id);
                userAccountabilityList.asset_ids = string.Join(",", existingAssetIds);
                _context.user_accountability_lists.Update(userAccountabilityList);
            }

            await _context.SaveChangesAsync();
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


        //for upload image endpoint 
        public async Task<string> UploadAssetImageAsync(int assetId, IFormFile assetImage)
        {
            // Allowed file extensions for images
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(assetImage.FileName).ToLower();

            // Check if the file extension is allowed
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Only images are allowed.");
            }

            // Find the asset in the database
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null)
            {
                throw new KeyNotFoundException("Asset not found.");
            }

            // Construct the file name and path
            var fileName = $"{assetId}_{Path.GetFileName(assetImage.FileName)}";
            var directoryPath = @"C:\Users\JBARNADO\Desktop\ITAM\asset_image";
            var filePath = Path.Combine(directoryPath, fileName);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Save the image to the directory
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await assetImage.CopyToAsync(stream);
            }

            // Update the asset with the file path
            asset.asset_image = filePath;
            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();

            return filePath;
        }

        //for create-vacant-asset endpoint 
        public async Task<Asset> CreateVacantAssetAsync(CreateAssetDto assetDto)
        {
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

            // Create a new Asset object
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

            return asset;
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
