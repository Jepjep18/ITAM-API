using IT_ASSET.DTOs;
using IT_ASSET.Models;
using IT_ASSET.Models.Logs;
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


    private string GetNextAccountabilityCode()
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

    private string GetNextTrackingCode()
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

            var lastAccountabilityCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.accountability_code)
                .Select(ual => ual.accountability_code)
                .FirstOrDefault();
            int accountabilityCodeCounter = 1;
            if (!string.IsNullOrEmpty(lastAccountabilityCode))
            {
                var lastNumber = lastAccountabilityCode.Substring(lastAccountabilityCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out var lastNumberParsed))
                {
                    accountabilityCodeCounter = lastNumberParsed + 1;
                }
            }

            var lastTrackingCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.tracking_code)
                .Select(ual => ual.tracking_code)
                .FirstOrDefault();
            int trackingCodeCounter = 1;
            if (!string.IsNullOrEmpty(lastTrackingCode))
            {
                var lastNumber = lastTrackingCode.Substring(lastTrackingCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out var lastNumberParsed))
                {
                    trackingCodeCounter = lastNumberParsed + 1;
                }
            }

            for (int row = 3; row <= rowCount; row++)
            {
                var user_name = worksheet.Cells[row, 1].Text.Trim();
                var company = worksheet.Cells[row, 2].Text.Trim();
                var department = worksheet.Cells[row, 3].Text.Trim();

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
                    await _context.SaveChangesAsync();
                }

                string date_acquired = string.Empty;
                var acqDateCellValue = worksheet.Cells[row, 5].Text.Trim();

                if (double.TryParse(acqDateCellValue, out var serialDate))
                {
                    var date = DateTime.FromOADate(serialDate);
                    date_acquired = date.ToString("MM/dd/yyyy");
                }
                else if (!string.IsNullOrWhiteSpace(acqDateCellValue))
                {
                    if (DateTime.TryParseExact(acqDateCellValue, "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        date_acquired = parsedDate.ToString("MM/dd/yyyy");
                    }
                    else
                    {
                        date_acquired = "Invalid Date";
                    }
                }

                var li_description = string.Join(" ",
                    worksheet.Cells[row, 7].Text?.Trim() ?? string.Empty,
                    worksheet.Cells[row, 4].Text?.Trim() ?? string.Empty,
                    worksheet.Cells[row, 8].Text?.Trim() ?? string.Empty,
                    worksheet.Cells[row, 9].Text?.Trim() ?? string.Empty,
                    worksheet.Cells[row, 10].Text?.Trim() ?? string.Empty,
                    worksheet.Cells[row, 11].Text?.Trim() ?? string.Empty,
                    worksheet.Cells[row, 12].Text?.Trim() ?? string.Empty,
                    worksheet.Cells[row, 13].Text?.Trim() ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(li_description))
                {
                    li_description = "No description available"; 
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
                    date_created = DateTime.UtcNow,
                    li_description = li_description 
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                var userAccountabilityList = await _context.user_accountability_lists
                    .FirstOrDefaultAsync(ual => ual.owner_id == user.id);

                if (userAccountabilityList == null)
                {
                    userAccountabilityList = new UserAccountabilityList
                    {
                        accountability_code = $"ACID-{accountabilityCodeCounter:D4}",
                        tracking_code = $"TRID-{trackingCodeCounter:D4}",
                        owner_id = user.id,
                        asset_ids = asset.id.ToString()
                    };
                    _context.user_accountability_lists.Add(userAccountabilityList);

                    accountabilityCodeCounter++;
                    trackingCodeCounter++;
                }
                else
                {
                    var existingAssetIds = userAccountabilityList.asset_ids.Split(',').Select(int.Parse).ToList();
                    existingAssetIds.Add(asset.id);
                    userAccountabilityList.asset_ids = string.Join(",", existingAssetIds);
                }

                await _context.SaveChangesAsync();
            }
        }

        return Ok("Assets imported successfully.");
    }











    [HttpPost("add-asset")]
    public async Task<IActionResult> AddAsset([FromBody] AddAssetDto assetDto)
    {
        if (assetDto == null)
        {
            return BadRequest("Invalid data.");
        }

        // Find or create the user
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

        // Generate li_description
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

        // Create and save the asset
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

        // Add or update user accountability list
        var userAccountabilityList = await _context.user_accountability_lists
            .FirstOrDefaultAsync(ual => ual.owner_id == user.id);

        if (userAccountabilityList == null)
        {
            // Generate new accountability and tracking codes
            var lastAccountabilityCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.accountability_code)
                .Select(ual => ual.accountability_code)
                .FirstOrDefault();

            int accountabilityCodeCounter = 1;
            if (!string.IsNullOrEmpty(lastAccountabilityCode))
            {
                var lastNumber = lastAccountabilityCode.Substring(lastAccountabilityCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out var lastNumberParsed))
                {
                    accountabilityCodeCounter = lastNumberParsed + 1;
                }
            }

            var lastTrackingCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.tracking_code)
                .Select(ual => ual.tracking_code)
                .FirstOrDefault();

            int trackingCodeCounter = 1;
            if (!string.IsNullOrEmpty(lastTrackingCode))
            {
                var lastNumber = lastTrackingCode.Substring(lastTrackingCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out var lastNumberParsed))
                {
                    trackingCodeCounter = lastNumberParsed + 1;
                }
            }

            // Create a new accountability list entry
            userAccountabilityList = new UserAccountabilityList
            {
                accountability_code = $"ACID-{accountabilityCodeCounter:D4}",
                tracking_code = $"TRID-{trackingCodeCounter:D4}",
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

        return Ok("Asset added successfully.");
    }





    [HttpPost("upload-image/{assetId}")]
        public async Task<IActionResult> UploadAssetImage(int assetId, IFormFile assetImage)
        {
            if (assetImage == null || assetImage.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(assetImage.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Invalid file type. Only images are allowed.");
            }

            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null)
            {
                return NotFound("Asset not found.");
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

            return Ok(new { message = "Image uploaded successfully.", filePath = filePath });
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
            li_description = string.Join(" ",
                assetDto.brand?.Trim(),
                assetDto.type?.Trim(),
                assetDto.model?.Trim(),
                assetDto.ram?.Trim(),
                assetDto.storage?.Trim(),
                assetDto.gpu?.Trim(),
                assetDto.size?.Trim(),
                assetDto.color?.Trim()).Trim(),
            date_acquired = assetDto.date_acquired,
            asset_image = assetDto.asset_image,
            owner_id = null,
            history = new List<string>(),

            date_created = DateTime.UtcNow 
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Asset created successfully.", asset });
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
