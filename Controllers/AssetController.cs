using IT_ASSET.DTOs;
using IT_ASSET.Models;
using IT_ASSET.Models.Logs;
using IT_ASSET.Services.ComputerService;
using IT_ASSET.Services.NewFolder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class AssetsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AssetImportService _assetImportService;
    private readonly AssetService _assetService;
    private readonly UserService _userService;
    private readonly ComputerService _computerService;



    public AssetsController(AppDbContext context , AssetImportService assetImportService, AssetService assetService, UserService userService, ComputerService computerService)
    {
        _context = context;
        _assetImportService = assetImportService;
        _assetService = assetService;
        _userService = userService;
        _computerService = computerService;

    }

    //store items from import for assets db tbl and computers db tbl
    [HttpPost("import")]
    public async Task<IActionResult> ImportAssets(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        try
        {
            var result = await _assetImportService.ImportAssetsAsync(file);
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error importing assets: {ex.Message}" });
        }
    }


    //add either assets item or computer item based on type
    [HttpPost("add-asset/computer")]
    public async Task<IActionResult> AddAsset([FromBody] AddAssetDto assetDto)
    {
        if (assetDto == null)
        {
            return BadRequest("Invalid data.");
        }

        try
        {
            var result = await _assetService.AddAssetAsync(assetDto);

            return Ok(result); 
        }
        catch (Exception ex)
        {
            return BadRequest($"Error adding asset: {ex.Message}");
        }
    }


    //upload image endpoint for asset items
    [HttpPost("upload-image/{assetId}")]
    public async Task<IActionResult> UploadAssetImage(int assetId, IFormFile assetImage)
    {
        if (assetImage == null || assetImage.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            var filePath = await _assetService.UploadAssetImageAsync(assetId, assetImage);

            return Ok(new { message = "Image uploaded successfully.", filePath });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error uploading image: {ex.Message}");
        }
    }


    //upload image endpoint for computer items
    [HttpPost("upload-computer-image/{computerId}")]
    public async Task<IActionResult> UploadComputerImage(int computerId, IFormFile computerImage)
    {
        if (computerImage == null || computerImage.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            var filePath = await _assetService.UploadComputerImageAsync(computerId, computerImage);

            return Ok(new { message = "Computer image uploaded successfully.", filePath });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error uploading image: {ex.Message}");
        }
    }

    //assign owner for vacant asset items
    [HttpPost("assign-owner-assets")]
    public async Task<IActionResult> AssignOwnerToVacantAsset([FromBody] AssignOwnerDto assignOwnerDto)
    {
        try
        {
            var asset = await _assetService.AssignOwnerToAssetAsync(assignOwnerDto);

            return Ok(new { message = "Owner assigned successfully to the asset.", asset });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error assigning owner: {ex.Message}");
        }
    }

    //assign owner for vacant computer items
    [HttpPost("assign-owner-computer")]
    public async Task<IActionResult> AssignOwnerToComputer([FromBody] AssignOwnerforComputerDto assignOwnerforComputerDto)
    {
        // Validate input
        if (assignOwnerforComputerDto == null || assignOwnerforComputerDto.computer_id == 0 || assignOwnerforComputerDto.owner_id == 0)
        {
            return BadRequest("Invalid data.");
        }

        try
        {
            var result = await _assetService.AssignOwnerToComputerAsync(assignOwnerforComputerDto);

            return Ok(new { message = "Owner assigned successfully to the computer.", result });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error assigning owner: {ex.Message}");
        }
    }


    //post endpoint for creating vacant item for asset and computer items store based on type
    [HttpPost("create-vacant-asset/computer-items")]
    public async Task<IActionResult> CreateVacantAsset([FromBody] CreateAssetDto assetDto)
    {
        if (assetDto == null)
        {
            return BadRequest("Invalid asset data.");
        }

        try
        {
            var asset = await _assetService.CreateVacantAssetAsync(assetDto);

            return Ok(new { message = "Asset created successfully.", asset });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating asset: {ex.Message}");
        }
    }




    //get endpoint to fetch assets and computer items based on owner id
    [HttpGet("owner/{owner_id}")]
    public async Task<IActionResult> GetAssetsByOwnerId(int owner_id)
    {
        try
        {
            var assets = await _context.Assets
                .Where(a => a.owner_id == owner_id)
                .ToListAsync();

            var computers = await _context.computers
                .Where(c => c.owner_id == owner_id)
                .ToListAsync();

            var combinedResults = new List<object>();
            combinedResults.AddRange(assets);
            combinedResults.AddRange(computers);

            if (combinedResults.Count == 0)
            {
                return NotFound(new { message = "No assets or computers found for this owner." });
            }

            return Ok(combinedResults);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving assets and computers: {ex.Message}");
        }
    }


    [HttpGet("type-filter-asset-computers/{type}")]
    public async Task<IActionResult> GetAssetsByType(
    string type,
    int pageNumber = 1,
    int pageSize = 10,
    string sortOrder = "asc",
    string? searchTerm = null)
    {
        try
        {
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


    [HttpGet("AssetItems")]
    public async Task<IActionResult> GetAllAssets(
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            try
            {
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

    [HttpGet("ComputerItems")]
    public async Task<IActionResult> GetAllComputers(
    int pageNumber = 1,
    int pageSize = 10,
    string sortOrder = "asc",
    string? searchTerm = null)
    {
        try
        {
            var response = await _computerService.GetAllComputersAsync(pageNumber, pageSize, sortOrder, searchTerm);

            if (response == null || !response.Items.Any())
            {
                return NotFound(new { message = "No computers found." });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving computers: {ex.Message}");
        }
    }



    [HttpGet("assets/vacant")]
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

    [HttpGet("computers/vacant")]
    public async Task<IActionResult> GetVacantComputers()
    {
        var vacantComputers = await _context.computers
            .Where(c => c.owner_id == null)
            .ToListAsync();

        if (vacantComputers == null || vacantComputers.Count == 0)
        {
            return NotFound(new { message = "No vacant computers available." });
        }

        return Ok(vacantComputers);
    }

    [HttpGet("AssetItems/{id}")]
    public async Task<IActionResult> GetAssetById(int id)
    {
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(id);

            if (asset == null)
            {
                return NotFound(new { message = $"Asset with ID {id} not found." });
            }

            return Ok(asset);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving asset: {ex.Message}");
        }
    }

    [HttpGet("Computers/{id}")]
    public async Task<IActionResult> GetComputerById(int id)
    {
        try
        {
            var computer = await _computerService.GetComputerByIdAsync(id);

            if (computer == null)
            {
                return NotFound(new { message = $"Computer with ID {id} not found." });
            }

            return Ok(computer);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving computer: {ex.Message}");
        }
    }


    //for get asset filename endpoint
    [HttpGet("asset-image/{filename}")]
    public IActionResult GetAssetImage(string filename)
    {
        try
        {
            var filePath = _assetService.GetAssetImageByFilenameAsync(filename).Result;

            Console.WriteLine($"Requested filename: '{filename}'");
            Console.WriteLine($"Looking for file at: {filePath}");

            var fileExtension = Path.GetExtension(filename).ToLowerInvariant();
            string contentType = fileExtension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var image = System.IO.File.OpenRead(filePath);
            return File(image, contentType);
        }
        catch (FileNotFoundException fnfEx)
        {
            return NotFound(new { message = fnfEx.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return StatusCode(500, new
            {
                message = "An error occurred while processing the request",
                error = ex.Message
            });
        }
    }



    //for get computer filename endpoint
    [HttpGet("computer-image/{filename}")]
    public IActionResult GetComputerImage(string filename)
    {
        try
        {
            var filePath = _computerService.GetComputerImageByFilenameAsync(filename).Result;

            Console.WriteLine($"Requested filename: '{filename}'");
            Console.WriteLine($"Looking for file at: {filePath}");

            var fileExtension = Path.GetExtension(filename).ToLowerInvariant();
            string contentType = fileExtension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var image = System.IO.File.OpenRead(filePath);
            return File(image, contentType);
        }
        catch (FileNotFoundException fnfEx)
        {
            return NotFound(new { message = fnfEx.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return StatusCode(500, new
            {
                message = "An error occurred while processing the request",
                error = ex.Message
            });
        }
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
            var ownerId = await _userService.GetOrCreateUserAsync(assetDto);

            // Access the ClaimsPrincipal from HttpContext.User
            var user = HttpContext.User;

            var updatedAsset = await _assetService.UpdateAssetAsync(asset_id, assetDto, ownerId, user);

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


    [HttpPut("update-computer/{computer_id}")]
    public async Task<IActionResult> UpdateComputer(int computer_id, [FromBody] UpdateComputerDto computerDto)
    {
        if (computerDto == null)
        {
            return BadRequest("Invalid data.");
        }

        try
        {
            var ownerId = await _userService.GetOrCreateUserAsync(computerDto);

            var updatedComputer = await _computerService.UpdateComputerAsync(computer_id, computerDto, ownerId, HttpContext.User);

            if (updatedComputer == null)
            {
                return NotFound(new { message = "Computer not found." });
            }

            return Ok(new { message = "Computer updated successfully.", computer = updatedComputer });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error updating computer: {ex.Message}");
        }
    }




    [HttpDelete("delete-asset/{id}")]
    public async Task<IActionResult> DeleteAsset(int id)
    {
        var user = HttpContext.User; // Get the logged-in user details

        var result = await _assetService.DeleteAssetAsync(id, user);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(new { message = "Asset deleted successfully.", assetId = id });
    }




    [HttpDelete("delete-computer/{id}")]
    public async Task<IActionResult> DeleteComputer(int id)
    {
        var user = HttpContext.User; // Get the logged-in user details

        var result = await _computerService.DeleteComputerAsync(id, user);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(new { message = "Computer deleted successfully.", computerId = id });
    }




}
