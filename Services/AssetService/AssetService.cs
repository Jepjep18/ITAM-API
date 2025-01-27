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
    }
}
