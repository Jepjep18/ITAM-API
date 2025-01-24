using IT_ASSET.DTOs;
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

    public AssetsController(AppDbContext context)
    {
        _context = context;
    }

        [HttpPost("import")]
        public async Task<IActionResult> ImportAssets(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

            
                for (int row = 3; row <= rowCount; row++) 
                {
                    var user_name = worksheet.Cells[row, 1].Text.Trim();  
                    var company = worksheet.Cells[row, 2].Text.Trim();  
                    var department = worksheet.Cells[row, 3].Text.Trim();

                    // Check if the user exists or create a new user
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.name == user_name && u.company == company && u.department == department);

                    if (user == null)
                    {
                        user = new User
                        {
                            name = user_name,
                            company = company,
                            department = department
                        };
                        _context.Users.Add(user);
                        await _context.SaveChangesAsync(); // Save user first
                    }

                    // Extract all history columns into a list
                    var history = new List<string>();
                    for (int col = 18; col <= 24; col++) // Assuming history columns are from 18 to 24
                    {
                        var history_value = worksheet.Cells[row, col].Text.Trim();
                        if (!string.IsNullOrWhiteSpace(history_value))
                        {
                            history.Add(history_value);
                        }
                    }

                    // Handle the date_acquired
                    string date_acquired = string.Empty; 

                    // Check if the acquisition date is valid or can be parsed
                    var acqDateCellValue = worksheet.Cells[row, 5].Text.Trim(); 

                    // If the cell contains a valid date serial number, convert it to a DateTime
                    if (double.TryParse(acqDateCellValue, out var serialDate))
                    {
                        // Convert Excel serial number to DateTime
                        var date = DateTime.FromOADate(serialDate);
                        date_acquired = date.ToString("MM/dd/yyyy");
                    }
                    // Else, attempt to parse as string date (MM/dd/yy)
                    else if (!string.IsNullOrWhiteSpace(acqDateCellValue))
                    {
                        if (DateTime.TryParseExact(acqDateCellValue, "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                        {
                            date_acquired = parsedDate.ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            // Fallback if it cannot be parsed correctly
                            date_acquired = "Invalid Date"; 
                        }
                    }

                    var asset = new Asset
                    {
                        type = worksheet.Cells[row, 4].Text.Trim(),
                        date_acquired = date_acquired,        
                        asset_barcode = worksheet.Cells[row, 6].Text.Trim(), 
                        brand = worksheet.Cells[row, 7].Text.Trim(),         
                        model = worksheet.Cells[row, 8].Text.Trim(),         
                        ram = worksheet.Cells[row, 9].Text.Trim(),           
                        storage = worksheet.Cells[row, 10].Text.Trim(),      
                        gpu = worksheet.Cells[row, 11].Text.Trim(),         
                        size = worksheet.Cells[row, 12].Text.Trim(),         
                        color = worksheet.Cells[row, 13].Text.Trim(),       
                        serial_no = worksheet.Cells[row, 14].Text.Trim(),    
                        po = worksheet.Cells[row, 15].Text.Trim(),           
                        warranty = worksheet.Cells[row, 16].Text.Trim(),     
                        cost = decimal.TryParse(worksheet.Cells[row, 17].Text.Trim(), out var cost) ? cost : 0, 
                        remarks = worksheet.Cells[row, 25].Text.Trim(),      
                        owner_id = user.id,                                  
                        history = history,                                   
                        li_description = string.Join(" ",                   
                            worksheet.Cells[row, 7].Text.Trim(),
                            worksheet.Cells[row, 4].Text.Trim(),
                            worksheet.Cells[row, 8].Text.Trim(),
                            worksheet.Cells[row, 9].Text.Trim(),
                            worksheet.Cells[row, 10].Text.Trim(),
                            worksheet.Cells[row, 11].Text.Trim(),
                            worksheet.Cells[row, 12].Text.Trim(),
                            worksheet.Cells[row, 13].Text.Trim()).Trim()
                    };

                    // Add Asset to the context
                    _context.Assets.Add(asset);
                }

                await _context.SaveChangesAsync();
            }

            return Ok("Assets imported successfully.");
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
        public async Task<IActionResult> GetAssetsByType(string type)
        {
            var assets = await _context.Assets
                .Where(a => a.type.ToLower() == type.ToLower())
                .ToListAsync();

            if (assets == null || assets.Count == 0)
            {
                return NotFound(new { message = $"No assets found for type: {type}." });
            }

            return Ok(assets);
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

        [HttpPost("add-asset")]
        public async Task<IActionResult> AddAsset([FromBody] AddAssetDto assetDto)
        {
            if (assetDto == null)
            {
                return BadRequest("Invalid data.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.name == assetDto.user_name && u.company == assetDto.company && u.department == assetDto.department);

            if (user == null)
            {
                user = new User
                {
                    name = assetDto.user_name,
                    company = assetDto.company,
                    department = assetDto.department,
                    employee_id = assetDto.employee_id  
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync(); 
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(assetDto.employee_id) && user.employee_id != assetDto.employee_id)
                {
                    user.employee_id = assetDto.employee_id;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync(); 
                }
            }
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
                history = assetDto.history,
                li_description = assetDto.li_description
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            return Ok("Asset added successfully.");
        }


        [HttpPut("update-asset/{asset_id}")]
        public async Task<IActionResult> UpdateAsset(int asset_id, [FromBody] AddAssetDto assetDto)
        {
            if (assetDto == null)
            {
                return BadRequest("Invalid data.");
            }

            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.id == asset_id);

            if (asset == null)
            {
                return NotFound(new { message = "Asset not found." });
            }

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
            asset.history = assetDto.history;
            asset.li_description = assetDto.li_description;

            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Asset updated successfully.", asset });
        }


    [HttpPost("upload-image/{assetId}")]
    public async Task<IActionResult> UploadAssetImage(int assetId, IFormFile assetImage)
    {
        if (assetImage == null || assetImage.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        // Validate file type (optional, depending on requirements)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(assetImage.FileName).ToLower();

        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest("Invalid file type. Only images are allowed.");
        }

        // Find the asset by id
        var asset = await _context.Assets.FindAsync(assetId);
        if (asset == null)
        {
            return NotFound("Asset not found.");
        }

        // Generate a unique filename for the uploaded image (or use something else like GUID)
        var fileName = $"{assetId}_{Path.GetFileName(assetImage.FileName)}";

        // Define the directory path (update this path as needed)
        var directoryPath = @"C:\Users\JBARNADO\Desktop\ITAM\asset_image";

        // Combine the directory path and filename to get the full file path
        var filePath = Path.Combine(directoryPath, fileName);

        // Ensure the directory exists
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Save the file to the specified directory
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await assetImage.CopyToAsync(stream);
        }

        // Update the asset's asset_image field with the file path
        asset.asset_image = filePath;  // Store the file path (you can use relative paths if needed)

        // Save changes to the database
        _context.Assets.Update(asset);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Image uploaded successfully.", filePath = filePath });
    }


    [HttpGet("vacant")]
    public async Task<IActionResult> GetVacantAssets()
    {
        // Query the assets that have no owner
        var vacantAssets = await _context.Assets
            .Where(a => a.owner_id == null)
            .ToListAsync();

        if (vacantAssets == null || vacantAssets.Count == 0)
        {
            return NotFound(new { message = "No vacant assets available." });
        }

        return Ok(vacantAssets);
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
            li_description = assetDto.li_description,
            date_acquired = assetDto.date_acquired,
            asset_image = assetDto.asset_image,
            owner_id = null, 
            history = new List<string>()  
        };

        // Add the asset to the context
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Asset created successfully.", asset });
    }


}
