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

            // Define the list of types to store in the Computer table
            var computerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPU",
                "CPU CORE i7 10th GEN",
                "CPU INTEL CORE i5",
                "Laptop",
                "Laptop Macbook AIR, NB 15S-DUI537TU"
            };

            // Initialize counters for accountability and tracking codes
            int accountabilityCodeCounter = 1;
            int trackingCodeCounter = 1;

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    if (IsRowEmpty(worksheet, row))
                        continue;

                    var assetType = GetCellValue(worksheet.Cells[row, 4]);

                    // Skip row if type is null, empty, or invalid
                    if (string.IsNullOrWhiteSpace(assetType))
                        continue;

                    var user = await EnsureUserAsync(worksheet, row);

                    var dateAcquired = ParseDate(GetCellValue(worksheet.Cells[row, 5]));
                    var liDescription = BuildDescription(worksheet, row);

                    // Get history data from columns 19 to 25
                    var history = new List<string>
                    {
                        GetCellValue(worksheet.Cells[row, 19]),
                        GetCellValue(worksheet.Cells[row, 20]),
                        GetCellValue(worksheet.Cells[row, 21]),
                        GetCellValue(worksheet.Cells[row, 22]),
                        GetCellValue(worksheet.Cells[row, 23]),
                        GetCellValue(worksheet.Cells[row, 24]),
                        GetCellValue(worksheet.Cells[row, 25])
                    };

                    // Remove any null or empty history values
                    history.RemoveAll(item => string.IsNullOrWhiteSpace(item));

                    if (computerTypes.Contains(assetType))
                    {
                        Computer computer = null;  // Declare computer outside try block
                        try
                        {
                            // Create Computer object
                            computer = new Computer
                            {
                                type = assetType,
                                date_acquired = dateAcquired,
                                asset_barcode = GetCellValue(worksheet.Cells[row, 6]),
                                brand = GetCellValue(worksheet.Cells[row, 7]),
                                model = GetCellValue(worksheet.Cells[row, 8]),
                                ram = GetCellValue(worksheet.Cells[row, 9]),
                                ssd = GetCellValue(worksheet.Cells[row, 10]),
                                hdd = GetCellValue(worksheet.Cells[row, 11]),
                                gpu = GetCellValue(worksheet.Cells[row, 12]),
                                size = GetCellValue(worksheet.Cells[row, 13]),
                                color = GetCellValue(worksheet.Cells[row, 14]),
                                serial_no = GetCellValue(worksheet.Cells[row, 15]),
                                po = GetCellValue(worksheet.Cells[row, 16]),
                                warranty = GetCellValue(worksheet.Cells[row, 17]),
                                cost = TryParseDecimal(worksheet.Cells[row, 18]) ?? 0,
                                remarks = GetCellValue(worksheet.Cells[row, 26]),
                                owner_id = user.id,
                                date_created = DateTime.UtcNow,
                                li_description = liDescription,
                                history = history
                            };

                            _context.computers.Add(computer);
                            await _context.SaveChangesAsync();

                            // Log the computer creation
                            await LogComputerActionAsync(computer, "Computer Imported", user.id,
                                $"Computer of type {computer.type} with barcode {computer.asset_barcode} imported.");

                            // After storing the computer, store the components in computer_components table
                            await StoreInComputerComponentsAsync(worksheet, row, assetType, user, computer);

                            // Update the UserAccountabilityList with accountability and tracking codes for Computer
                            var (updatedAccountabilityCodeCounter, updatedTrackingCodeCounter) =
                                await UpdateUserAccountabilityListAsync(user, computer, accountabilityCodeCounter, trackingCodeCounter);

                            // Update the counters
                            accountabilityCodeCounter = updatedAccountabilityCodeCounter;
                            trackingCodeCounter = updatedTrackingCodeCounter;
                        }
                        catch (DbUpdateException dbEx)
                        {
                            var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                            var errorDetails = computer != null
                                ? $"Error saving computer of type {computer.type} with barcode {computer.asset_barcode}: {innerException}"
                                : $"Error saving computer: {innerException}";
                            throw new Exception(errorDetails);
                        }
                    }
                    else
                    {
                        try
                        {
                            // Store in Asset table
                            var asset = new Asset
                            {
                                type = assetType,
                                date_acquired = dateAcquired,
                                asset_barcode = GetCellValue(worksheet.Cells[row, 6]),
                                brand = GetCellValue(worksheet.Cells[row, 7]),
                                model = GetCellValue(worksheet.Cells[row, 8]),
                                ram = GetCellValue(worksheet.Cells[row, 9]),
                                ssd = GetCellValue(worksheet.Cells[row, 10]),
                                hdd = GetCellValue(worksheet.Cells[row, 11]),
                                gpu = GetCellValue(worksheet.Cells[row, 12]),
                                size = GetCellValue(worksheet.Cells[row, 13]),
                                color = GetCellValue(worksheet.Cells[row, 14]),
                                serial_no = GetCellValue(worksheet.Cells[row, 15]),
                                po = GetCellValue(worksheet.Cells[row, 16]),
                                warranty = GetCellValue(worksheet.Cells[row, 17]),
                                cost = TryParseDecimal(worksheet.Cells[row, 18]) ?? 0,
                                remarks = GetCellValue(worksheet.Cells[row, 26]),
                                owner_id = user.id,
                                date_created = DateTime.UtcNow,
                                li_description = liDescription,
                                history = history
                            };

                            _context.Assets.Add(asset);
                            await _context.SaveChangesAsync();

                            // Log the asset creation
                            await LogAssetActionAsync(asset, "Asset Imported", user.id,
                                $"Asset of type {asset.type} with barcode {asset.asset_barcode} imported.");

                            // Update the UserAccountabilityList with accountability and tracking codes for Asset
                            var (updatedAccountabilityCodeCounter, updatedTrackingCodeCounter) =
                                await UpdateUserAccountabilityListAsync(user, asset, accountabilityCodeCounter, trackingCodeCounter);

                            // Update the counters
                            accountabilityCodeCounter = updatedAccountabilityCodeCounter;
                            trackingCodeCounter = updatedTrackingCodeCounter;
                        }
                        catch (DbUpdateException dbEx)
                        {
                            var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                            throw new Exception($"Error saving asset: {innerException}");
                        }
                    }
                }
            }

            return "Import completed successfully.";
        }




        private bool IsInvalidRow(ExcelWorksheet worksheet, int row)
        {
            // Check if the entire row is empty
            if (IsRowEmpty(worksheet, row))
                return true;

            // Check if essential columns are empty
            var essentialColumns = new[] { 4, 6, 7, 8 }; // Columns for type, barcode, brand, model
            foreach (var col in essentialColumns)
            {
                if (string.IsNullOrWhiteSpace(GetCellValue(worksheet.Cells[row, col])))
                    return true;
            }

            return false;
        }


        // Helper to check if a row is empty
        private bool IsRowEmpty(ExcelWorksheet worksheet, int row)
        {
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text))
                    return false;
            }
            return true;
        }

        // Helper to safely get a cell value or return null
        private string GetCellValue(ExcelRange cell)
        {
            var value = cell.Text.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        // Helper to safely parse a decimal or return null
        private decimal? TryParseDecimal(ExcelRange cell)
        {
            return decimal.TryParse(cell.Text.Trim(), out var result) ? result : (decimal?)null;
        }



        // Method to store components like RAM, SSD, etc., in the ComputerComponents table
        private async Task StoreInComputerComponentsAsync(ExcelWorksheet worksheet, int row, string assetType, User user, Computer computer)
        {
            var assetBarcode = worksheet.Cells[row, 6].Text.Trim(); // Assuming barcode is in column 6
            var ownerId = user.id;

            // Define headers that you want to store as 'type'
            var headers = new string[] { "RAM", "SSD", "HDD", "GPU" };
            var values = new string[]
            {
            worksheet.Cells[row, 9].Text.Trim(),  // 'RAM'
            worksheet.Cells[row, 10].Text.Trim(), // 'SSD'
            worksheet.Cells[row, 11].Text.Trim(), // 'HDD'
            worksheet.Cells[row, 12].Text.Trim(), // 'GPU'
            };

            // Extract history data from columns 19 to 25
            var history = new List<string>
            {
                worksheet.Cells[row, 19].Text.Trim(),
                worksheet.Cells[row, 20].Text.Trim(),
                worksheet.Cells[row, 21].Text.Trim(),
                worksheet.Cells[row, 22].Text.Trim(),
                worksheet.Cells[row, 23].Text.Trim(),
                worksheet.Cells[row, 24].Text.Trim(),
                worksheet.Cells[row, 25].Text.Trim()
            };

            // Remove empty or null entries from history
            history.RemoveAll(item => string.IsNullOrWhiteSpace(item));

            // Loop through the headers and values to create components
            for (int i = 0; i < headers.Length; i++)
            {
                var description = values[i];

                // Only create a component if the description is not empty
                if (!string.IsNullOrWhiteSpace(description))
                {
                    var component = new ComputerComponents
                    {
                        type = headers[i],
                        description = description,
                        asset_barcode = assetBarcode,
                        status = ownerId != null ? "Released" : "New",
                        history = new List<string>(history), // Assign the same history data to each component
                        owner_id = ownerId,
                        computer_id = computer.id // Set the computer_id foreign key
                    };

                    _context.computer_components.Add(component);
                    Console.WriteLine($"Adding {component.type}: {component.description}");
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

        private async Task LogComputerActionAsync(Computer computer, string action, int performedByUserId, string details)
        {
            var log = new Computer_logs
            {
                computer_id = computer.id,
                action = action,
                performed_by_user_id = performedByUserId.ToString(),
                timestamp = DateTime.UtcNow,
                details = details
            };

            _context.computer_Logs.Add(log);
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



        private async Task<(int AccountabilityCodeCounter, int TrackingCodeCounter)> UpdateUserAccountabilityListAsync(
        User user, object assetOrComputer, int accountabilityCodeCounter, int trackingCodeCounter)
        {
            int assetId = 0;
            int computerId = 0;

            if (assetOrComputer is Asset asset)
            {
                assetId = asset.id; // Get Asset ID
            }
            else if (assetOrComputer is Computer computer)
            {
                computerId = computer.id; // Get Computer ID
            }
            else
            {
                throw new ArgumentException("Invalid asset or computer type.");
            }

            var userAccountabilityList = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ual => ual.owner_id == user.id);

            if (userAccountabilityList == null)
            {
                // Ensure asset_ids and computer_ids are not null and provide default empty string values
                userAccountabilityList = new UserAccountabilityList
                {
                    accountability_code = $"ACID-{accountabilityCodeCounter:D4}",
                    tracking_code = $"TRID-{trackingCodeCounter:D4}",
                    owner_id = user.id,
                    asset_ids = assetId > 0 ? assetId.ToString() : "",  // Use empty string if no asset
                    computer_ids = computerId > 0 ? computerId.ToString() : ""  // Use empty string if no computer
                };
                _context.user_accountability_lists.Add(userAccountabilityList);

                accountabilityCodeCounter++;
                trackingCodeCounter++;
            }
            else
            {
                // Safely parse asset_ids if they are not empty
                if (!string.IsNullOrEmpty(userAccountabilityList.asset_ids))
                {
                    var existingAssetIds = userAccountabilityList.asset_ids
                        .Split(',')
                        .Where(id => !string.IsNullOrWhiteSpace(id)) // Ensure non-empty entries
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0) // Safely parse IDs
                        .Where(id => id > 0) // Filter out invalid or 0 IDs
                        .ToList();

                    if (assetId > 0)
                    {
                        existingAssetIds.Add(assetId);
                    }

                    userAccountabilityList.asset_ids = string.Join(",", existingAssetIds);
                }
                else
                {
                    // If asset_ids is empty, initialize as an empty list or the current assetId
                    if (assetId > 0)
                    {
                        userAccountabilityList.asset_ids = assetId.ToString();
                    }
                }

                // Safely parse computer_ids if they are not empty
                if (!string.IsNullOrEmpty(userAccountabilityList.computer_ids))
                {
                    var existingComputerIds = userAccountabilityList.computer_ids
                        .Split(',')
                        .Where(id => !string.IsNullOrWhiteSpace(id)) // Ensure non-empty entries
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0) // Safely parse IDs
                        .Where(id => id > 0) // Filter out invalid or 0 IDs
                        .ToList();

                    if (computerId > 0)
                    {
                        existingComputerIds.Add(computerId);
                    }

                    userAccountabilityList.computer_ids = string.Join(",", existingComputerIds);
                }
                else
                {
                    // If computer_ids is empty, initialize as an empty list or the current computerId
                    if (computerId > 0)
                    {
                        userAccountabilityList.computer_ids = computerId.ToString();
                    }
                }
            }

            await _context.SaveChangesAsync();

            return (accountabilityCodeCounter, trackingCodeCounter);
        }




    }
}
