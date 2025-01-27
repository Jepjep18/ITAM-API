using IT_ASSET.DTOs;
using IT_ASSET.Models;
using IT_ASSET.Models.Logs;
using IT_ASSET.Services.NewFolder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class AssetsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AssetImportService _assetImportService;
    private readonly AssetService _assetService;
    private readonly UserService _userService;



    public AssetsController(AppDbContext context , AssetImportService assetImportService, AssetService assetService, UserService userService)
    {
        _context = context;
        _assetImportService = assetImportService;
        _assetService = assetService;
        _userService = userService;

    }




    [HttpPost("import")]
    public async Task<IActionResult> ImportAssets(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            // Delegate the logic of importing assets to the service
            var result = await _assetImportService.ImportAssetsAsync(file);

            return Ok(result); // Return the success message from the service
        }
        catch (Exception ex)
        {
            return BadRequest($"Error importing assets: {ex.Message}");
        }
    }



    [HttpPost("add-asset")]
    public async Task<IActionResult> AddAsset([FromBody] AddAssetDto assetDto)
    {
        if (assetDto == null)
        {
            return BadRequest("Invalid data.");
        }

        try
        {
            // Call the service to handle adding the asset
            var result = await _assetService.AddAssetAsync(assetDto);

            return Ok(result); // Return the success message
        }
        catch (Exception ex)
        {
            return BadRequest($"Error adding asset: {ex.Message}");
        }
    }





    [HttpPost("upload-image/{assetId}")]
    public async Task<IActionResult> UploadAssetImage(int assetId, IFormFile assetImage)
    {
        if (assetImage == null || assetImage.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            // Call the service to handle the image upload
            var filePath = await _assetService.UploadAssetImageAsync(assetId, assetImage);

            return Ok(new { message = "Image uploaded successfully.", filePath });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error uploading image: {ex.Message}");
        }
    }


    [HttpPost("assign-owner")]
        public async Task<IActionResult> AssignOwnerToVacantAsset([FromBody] AssignOwnerDto assignOwnerDto)
        {
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.id == assignOwnerDto.AssetId && a.owner_id == null);

            if (asset == null)
            {
                return NotFound(new { message = "Vacant asset not found or already has an owner." });
            }

            var user = await _context.Users.FindAsync(assignOwnerDto.OwnerId);
            if (user == null)
            {
                return NotFound(new { message = "Owner not found." });
            }

            asset.owner_id = assignOwnerDto.OwnerId;

            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Owner assigned successfully to the asset.", asset });
        }


    [HttpPost("create-vacant-asset")]
    public async Task<IActionResult> CreateVacantAsset([FromBody] CreateAssetDto assetDto)
    {
        if (assetDto == null)
        {
            return BadRequest("Invalid asset data.");
        }

        try
        {
            // Call the service to create the vacant asset
            var asset = await _assetService.CreateVacantAssetAsync(assetDto);

            return Ok(new { message = "Asset created successfully.", asset });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating asset: {ex.Message}");
        }
    }





    [HttpGet("owner/{owner_id}")]
        public async Task<IActionResult> GetAssetsByOwnerId(int owner_id)
        {
        
            var assets = await _context.Assets
                .Where(a => a.owner_id == owner_id)
                .ToListAsync();

        
            if (assets == null || assets.Count == 0)
            {
                return NotFound(new { message = "No assets found for this owner." });
            }

        
            return Ok(assets);
        }


        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetAssetsByType(
        string type,
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            var query = _context.Assets
                .Where(a => a.type.ToLower() == type.ToLower())
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(asset =>
                    asset.asset_barcode.Contains(searchTerm) ||
                    asset.type.Contains(searchTerm) ||
                    asset.brand.Contains(searchTerm));
            }

            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(asset => asset.id),
                "asc" or _ => query.OrderBy(asset => asset.id),
            };

            var totalItems = await query.CountAsync();

            var paginatedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!paginatedData.Any())
            {
                return NotFound(new { message = $"No assets found for type: {type}." });
            }

            var response = new PaginatedResponse<Asset>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAssets(
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            var query = _context.Assets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(asset =>
                    asset.asset_barcode.Contains(searchTerm) ||
                    asset.type.Contains(searchTerm) ||
                    asset.brand.Contains(searchTerm));
            }

            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(asset => asset.id),
                "asc" or _ => query.OrderBy(asset => asset.id),
            };

            var totalItems = await query.CountAsync();
            var paginatedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PaginatedResponse<Asset>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(response);
        }


        [HttpGet("vacant")]
        public async Task<IActionResult> GetVacantAssets()
        {
            var vacantAssets = await _context.Assets
                .Where(a => a.owner_id == null)
                .ToListAsync();

            if (vacantAssets == null || vacantAssets.Count == 0)
            {
                return NotFound(new { message = "No vacant assets available." });
            }

            return Ok(vacantAssets);
        }

    [HttpPut("update-asset/{asset_id}")]
    public async Task<IActionResult> UpdateAsset(int asset_id, [FromBody] UpdateAssetDto assetDto)
    {
        if (assetDto == null)
        {
            return BadRequest("Invalid data.");
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.name == assetDto.user_name
                                   && u.company == assetDto.company
                                   && u.department == assetDto.department
                                   && u.employee_id == assetDto.employee_id);

        int ownerId;
        if (existingUser != null)
        {
            ownerId = existingUser.id;  
        }
        else
        {
            var newUser = new User
            {
                name = assetDto.user_name,  
                company = assetDto.company,
                department = assetDto.department,
                employee_id = assetDto.employee_id
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            ownerId = newUser.id;
        }

        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.id == asset_id);

        if (asset == null)
        {
            return NotFound(new { message = "Asset not found." });
        }

        if (asset.owner_id != ownerId)
        {
            if (asset.history == null)
            {
                asset.history = new List<string>();
            }

            var previousOwnerName = await _context.Users
                .Where(u => u.id == asset.owner_id)
                .Select(u => u.name)
                .FirstOrDefaultAsync();

            string previousOwner = previousOwnerName ?? "Unknown";

            string newHistoryEntry = $"{previousOwner}";

            asset.history.Add(newHistoryEntry);
        }

        asset.li_description = string.Join(" ",
            assetDto.brand?.Trim(),
            assetDto.type?.Trim(),
            assetDto.model?.Trim(),
            assetDto.ram?.Trim(),
            assetDto.storage?.Trim(),
            assetDto.gpu?.Trim(),
            assetDto.size?.Trim(),
            assetDto.color?.Trim()).Trim();

            asset.type = assetDto.type;
            asset.date_acquired = assetDto.date_acquired;
            asset.asset_barcode = assetDto.asset_barcode;
            asset.brand = assetDto.brand;
            asset.model = assetDto.model;
            asset.ram = assetDto.ram;
            asset.storage = assetDto.storage;
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

            return Ok(new { message = "Asset updated successfully.", asset });
        }

    [HttpDelete("delete-asset/{id}")]
    public async Task<IActionResult> DeleteAsset(int id)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.id == id);

        if (asset == null)
        {
            return NotFound(new { message = "Asset not found." });
        }

        if (asset.is_deleted)
        {
            return Conflict(new { message = "Asset is already deleted." });
        }

        asset.is_deleted = true;
        asset.date_modified = DateTime.UtcNow; 

        _context.Assets.Update(asset);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Asset deleted successfully.", assetId = id });
    }

}
