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
                storage = assetDto.storage,
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
                assetDto.storage?.Trim(),
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
                assetDto.storage?.Trim(),
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
                storage = assetDto.storage,
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
    }
}
