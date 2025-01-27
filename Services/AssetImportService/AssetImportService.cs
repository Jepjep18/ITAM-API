using IT_ASSET.DTOs;
using IT_ASSET.Models;
using IT_ASSET.Models.Logs;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;

namespace IT_ASSET.Services.NewFolder
{
    public class AssetImportService
    {
        private readonly AppDbContext _context;

        public AssetImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> ImportAssetsAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            // Define the list of excluded asset types
            var excludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPU",
                "CPU CORE i7 10th GEN",
                "CPU INTEL CORE i5",
                "Laptop",
                "Laptop Macbook AIR",
                "NB 15S-DUI537TU"
            };

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                int accountabilityCodeCounter = GetNextCodeCounter("accountability_code", "ACID-");
                int trackingCodeCounter = GetNextCodeCounter("tracking_code", "TRID-");

                for (int row = 3; row <= rowCount; row++)
                {
                    var user = await EnsureUserAsync(worksheet, row);

                    var dateAcquired = ParseDate(worksheet.Cells[row, 5].Text.Trim());
                    var liDescription = BuildDescription(worksheet, row);

                    var assetType = worksheet.Cells[row, 4].Text.Trim();

                    // If the asset type is in the excluded types list (like LAPTOP or CPU), store in computer components table
                    if (excludedTypes.Contains(assetType))
                    {
                        // Store in Computer table
                        await StoreInComputerComponentsAsync(worksheet, row, assetType, user);
                        continue; // Skip adding it to Assets table
                    }

                    // Create and store the asset in the Assets table if it doesn't match the excluded types
                    var asset = new Asset
                    {
                        type = assetType,
                        date_acquired = dateAcquired,
                        asset_barcode = worksheet.Cells[row, 6].Text.Trim(),
                        brand = worksheet.Cells[row, 7].Text.Trim(),
                        model = worksheet.Cells[row, 8].Text.Trim(),
                        ram = worksheet.Cells[row, 9].Text.Trim(),
                        ssd = worksheet.Cells[row, 10].Text.Trim(),
                        hdd = worksheet.Cells[row, 11].Text.Trim(),
                        gpu = worksheet.Cells[row, 12].Text.Trim(),
                        size = worksheet.Cells[row, 13].Text.Trim(),
                        color = worksheet.Cells[row, 14].Text.Trim(),
                        serial_no = worksheet.Cells[row, 15].Text.Trim(),
                        po = worksheet.Cells[row, 16].Text.Trim(),
                        warranty = worksheet.Cells[row, 17].Text.Trim(),
                        cost = decimal.TryParse(worksheet.Cells[row, 18].Text.Trim(), out var cost) ? cost : 0,
                        remarks = worksheet.Cells[row, 26].Text.Trim(),
                        owner_id = user.id,
                        date_created = DateTime.UtcNow,
                        li_description = liDescription
                    };

                    _context.Assets.Add(asset);
                    await _context.SaveChangesAsync();

                    // Log the asset creation
                    await LogAssetActionAsync(asset, "Asset Imported", user.id, $"Asset imported with barcode {asset.asset_barcode}.");

                    // Update user accountability list and get updated counters
                    var (updatedAccountabilityCodeCounter, updatedTrackingCodeCounter) =
                        await UpdateUserAccountabilityListAsync(user, asset, accountabilityCodeCounter, trackingCodeCounter);

                    // Log the user accountability update
                    await LogUserActionAsync(user, "User Accountability Updated", user.id, $"Asset {asset.id} assigned to user {user.name}.");

                    // Update the counters after the method call
                    accountabilityCodeCounter = updatedAccountabilityCodeCounter;
                    trackingCodeCounter = updatedTrackingCodeCounter;
                }
            }

            return "Assets imported successfully.";
        }

        private async Task LogAssetActionAsync(Asset asset, string action, int performedByUserId, string details)
        {
            var log = new Asset_logs
            {
                asset_id = asset.id,
                action = action,
                performed_by_user_id = performedByUserId.ToString(),
                timestamp = DateTime.UtcNow,
                details = details
            };

            _context.asset_Logs.Add(log);
            await _context.SaveChangesAsync();
        }

        private async Task LogUserActionAsync(User user, string action, int performedByUserId, string details)
        {
            var log = new User_logs
            {
                user_id = user.id,
                action = action,
                performed_by_user_id = performedByUserId.ToString(),
                timestamp = DateTime.UtcNow,
                details = details
            };

            _context.user_logs.Add(log);
            await _context.SaveChangesAsync();
        }

        private int GetNextCodeCounter(string column, string prefix)
        {
            var lastCode = _context.user_accountability_lists
                .OrderByDescending(ual => EF.Property<string>(ual, column))
                .Select(ual => EF.Property<string>(ual, column))
                .FirstOrDefault();

            if (string.IsNullOrEmpty(lastCode)) return 1;

            var lastNumber = lastCode.Substring(lastCode.LastIndexOf('-') + 1);
            return int.TryParse(lastNumber, out var lastNumberParsed) ? lastNumberParsed + 1 : 1;
        }

        private async Task<User> EnsureUserAsync(ExcelWorksheet worksheet, int row)
        {
            var userName = worksheet.Cells[row, 1].Text.Trim();
            var company = worksheet.Cells[row, 2].Text.Trim();
            var department = worksheet.Cells[row, 3].Text.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.name == userName && u.company == company && u.department == department);

            if (user == null)
            {
                user = new User
                {
                    name = userName,
                    company = company,
                    department = department
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            return user;
        }

        private string ParseDate(string dateCellValue)
        {
            if (double.TryParse(dateCellValue, out var serialDate))
            {
                var date = DateTime.FromOADate(serialDate);
                return date.ToString("MM/dd/yyyy");
            }
            else if (!string.IsNullOrWhiteSpace(dateCellValue))
            {
                if (DateTime.TryParseExact(dateCellValue, "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    return parsedDate.ToString("MM/dd/yyyy");
                }
            }

            return "Invalid Date";
        }

        private string BuildDescription(ExcelWorksheet worksheet, int row)
        {
            var descriptionParts = new[] {
                worksheet.Cells[row, 7].Text?.Trim(),
                worksheet.Cells[row, 4].Text?.Trim(),
                worksheet.Cells[row, 8].Text?.Trim(),
                worksheet.Cells[row, 9].Text?.Trim(),
                worksheet.Cells[row, 10].Text?.Trim(),
                worksheet.Cells[row, 11].Text?.Trim(),
                worksheet.Cells[row, 12].Text?.Trim(),
                worksheet.Cells[row, 13].Text?.Trim(),
                worksheet.Cells[row, 14].Text?.Trim()
            };

            return string.Join(" ", descriptionParts.Where(part => !string.IsNullOrWhiteSpace(part))).Trim() ?? "No description available";
        }

        private async Task StoreInComputerComponentsAsync(ExcelWorksheet worksheet, int row, string assetType, User user)
        {
            var assetBarcode = worksheet.Cells[row, 6].Text.Trim(); // Assuming barcode is in column 6
            var ownerId = user.id;

            // Define headers that you want to store as 'type'
            var headers = new string[] { "TYPE", "BRAND", "MODEL", "RAM", "SSD", "HDD", "GPU", "SIZE", "COLOR" };
            var values = new string[] {
                assetType, // 'TYPE' value from the Excel data
                worksheet.Cells[row, 7].Text.Trim(),  // 'BRAND'
                worksheet.Cells[row, 8].Text.Trim(),  // 'MODEL'
                worksheet.Cells[row, 9].Text.Trim(),  // 'RAM'
                worksheet.Cells[row, 10].Text.Trim(), // 'SSD'
                worksheet.Cells[row, 11].Text.Trim(), // 'HDD'
                worksheet.Cells[row, 12].Text.Trim(), // 'GPU'
                worksheet.Cells[row, 13].Text.Trim(), // 'SIZE'
                worksheet.Cells[row, 14].Text.Trim()  // 'COLOR'
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var component = new ComputerComponents
                {
                    type = headers[i],
                    description = values[i],
                    asset_barcode = assetBarcode,
                    status = ownerId != null ? "Released" : "New",
                    history = new List<string> { values[i] },
                    owner_id = ownerId
                };

                _context.computer_components.Add(component);
                Console.WriteLine($"Adding {component.type}: {component.description}"); // Debug log
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

        private async Task<(int AccountabilityCodeCounter, int TrackingCodeCounter)> UpdateUserAccountabilityListAsync(
            User user, Asset asset, int accountabilityCodeCounter, int trackingCodeCounter)
        {
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

            return (accountabilityCodeCounter, trackingCodeCounter);
        }
    }
}
