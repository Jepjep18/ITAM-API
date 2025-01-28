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

    [HttpPost("upload-computer-image/{computerId}")]
    public async Task<IActionResult> UploadComputerImage(int computerId, IFormFile computerImage)
    {
        if (computerImage == null || computerImage.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            // Call the service to handle the image upload
            var filePath = await _assetService.UploadComputerImageAsync(computerId, computerImage);

            return Ok(new { message = "Computer image uploaded successfully.", filePath });
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
        try
        {
            // Delegate the logic to the AssetService
            var response = await _assetService.GetAssetsByTypeAsync(
                type, pageNumber, pageSize, sortOrder, searchTerm);

            if (response == null || !response.Items.Any())
            {
                return NotFound(new { message = $"No assets found for type: {type}." });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving assets: {ex.Message}");
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetAllAssets(
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            try
            {
                // Delegate the logic to the AssetService
                var response = await _assetService.GetAllAssetsAsync(pageNumber, pageSize, sortOrder, searchTerm);

                if (response == null || !response.Items.Any())
                {
                    return NotFound(new { message = "No assets found." });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving assets: {ex.Message}");
            }
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

        try
        {
            // Step 1: Delegate user handling logic to UserService
            var ownerId = await _userService.GetOrCreateUserAsync(assetDto);

            // Step 2: Delegate asset update logic to AssetService
            var updatedAsset = await _assetService.UpdateAssetAsync(asset_id, assetDto, ownerId);

            if (updatedAsset == null)
            {
                return NotFound(new { message = "Asset not found." });
            }

            return Ok(new { message = "Asset updated successfully.", asset = updatedAsset });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error updating asset: {ex.Message}");
        }
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
