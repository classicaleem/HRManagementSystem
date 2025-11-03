using ClosedXML.Excel;
using HRManagementSystem.Data;
using HRManagementSystem.Models; // Assuming Company model is here
using HRManagementSystem.Models.CostsDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Text.Json;

namespace HRManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,HR,GM")]
    public class CostsDashboardController : Controller
    {
        private readonly ICostsDashboardRepository _costsDashboardRepository;
        private readonly ICompanyRepository _companyRepository;

        public CostsDashboardController(
            ICostsDashboardRepository costsDashboardRepository,
            ICompanyRepository companyRepository)
        {
            _costsDashboardRepository = costsDashboardRepository;
            _companyRepository = companyRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

            var companiesRaw = await _companyRepository.GetCompaniesByUserRoleAsync(GetRoleId(userRole), userCompanyCode);
            var companiesWithAll = new List<Company> { new Company { CompanyCode = 0, CompanyName = "ALL" } };
            companiesWithAll.AddRange(companiesRaw);

            var startDate = DateTime.Now.Date;
            var endDate = DateTime.Now.Date;

            // Fetch initial lists for dropdowns based on initial company
            var initialCompanyCodeForFilters = userCompanyCode;
            var categories = await _costsDashboardRepository.GetCategoriesByCompanyAsync(initialCompanyCodeForFilters);
            var mainCostTypes = await _costsDashboardRepository.GetMainCostTypesByCompanyAsync(initialCompanyCodeForFilters); // Fetch Main Types

            // Set default selections
            var selectedCategories = categories.Where(c => new List<string> { "WORKER", "GROWMORE", "Dailywages", "MALE-WORKER", "FTC", "DW-450" }.Contains(c)).ToList();
            var selectedMainCostTypes = new List<string>(); // Default to ALL

            // Fetch initial data based on defaults
            var initialSummary = await _costsDashboardRepository.GetCostsSummaryAsync(initialCompanyCodeForFilters, selectedCategories, selectedMainCostTypes, startDate, endDate);
            var initialAttendanceSummary = await _costsDashboardRepository.GetAttendanceSummaryAsync(initialCompanyCodeForFilters, selectedCategories, selectedMainCostTypes, startDate, endDate);
            var initialMainCostTypeSummaries = await _costsDashboardRepository.GetMainCostTypeSummaryAsync(initialCompanyCodeForFilters, selectedCategories, selectedMainCostTypes, startDate, endDate); // Fetch Main Type Summary
            var initialEmployeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(initialCompanyCodeForFilters, selectedCategories, selectedMainCostTypes, startDate, endDate);


            var model = new CostsDashboardViewModel
            {
                SelectedCompanyCode = initialCompanyCodeForFilters,
                SelectedCategories = selectedCategories,
                SelectedMainCostTypes = selectedMainCostTypes, // Bind to MainCostTypes
                StartDate = startDate,
                EndDate = endDate,
                Companies = new SelectList(companiesWithAll, "CompanyCode", "CompanyName", initialCompanyCodeForFilters),
                Categories = new MultiSelectList(categories, selectedCategories),
                MainCostTypes = new MultiSelectList(mainCostTypes, selectedMainCostTypes), // Bind to MainCostTypes
                Summary = initialSummary,
                AttendanceSummary = initialAttendanceSummary,
                MainCostTypeSummaries = initialMainCostTypeSummaries, // Bind to MainCostTypeSummaries
                EmployeeCosts = initialEmployeeCosts
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshData(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate) // Changed departments to mainCostTypes
        {
            try
            {
                categories ??= new List<string>();
                mainCostTypes ??= new List<string>(); // Treat null as ALL

                var summary = await _costsDashboardRepository.GetCostsSummaryAsync(companyCode, categories, mainCostTypes, startDate, endDate);
                var attendanceSummary = await _costsDashboardRepository.GetAttendanceSummaryAsync(companyCode, categories, mainCostTypes, startDate, endDate);
                var mainCostTypeSummaries = await _costsDashboardRepository.GetMainCostTypeSummaryAsync(companyCode, categories, mainCostTypes, startDate, endDate); // Fetch Main Type Summary
                var employeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(companyCode, categories, mainCostTypes, startDate, endDate);

                return Json(new
                {
                    success = true,
                    summary = summary,
                    attendanceSummary = attendanceSummary,
                    mainCostTypeSummaries = mainCostTypeSummaries, // Return Main Type Summary
                    employeeCosts = employeeCosts
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RefreshData: {ex.Message} \n {ex.StackTrace}");
                return Json(new { success = false, message = $"An error occurred while refreshing data." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesByCompany(int companyCode)
        {
            try { var data = await _costsDashboardRepository.GetCategoriesByCompanyAsync(companyCode); return Json(data.Select(c => new { value = c, text = c })); }
            catch (Exception ex) { Console.WriteLine($"Error GetCategoriesByCompany: {ex.Message}"); return Json(new List<object>()); }
        }

        // --- NEW Action to get Main Cost Types ---
        [HttpGet]
        public async Task<IActionResult> GetMainCostTypesByCompany(int companyCode)
        {
            try { var data = await _costsDashboardRepository.GetMainCostTypesByCompanyAsync(companyCode); return Json(data.Select(m => new { value = m, text = m })); }
            catch (Exception ex) { Console.WriteLine($"Error GetMainCostTypesByCompany: {ex.Message}"); return Json(new List<object>()); }
        }


        [HttpPost]
        public async Task<IActionResult> ExportToExcel(int companyCode, string categoriesJson, string mainCostTypesJson, DateTime startDate, DateTime endDate) // Changed param name
        {
            try
            {
                List<string> categories = new List<string>();
                if (!string.IsNullOrEmpty(categoriesJson) && categoriesJson != "[]") { try { categories = JsonSerializer.Deserialize<List<string>>(categoriesJson) ?? new List<string>(); } catch { /* ignore */ } }

                List<string> mainCostTypes = new List<string>(); // Changed variable name
                if (!string.IsNullOrEmpty(mainCostTypesJson) && mainCostTypesJson != "[]") { try { mainCostTypes = JsonSerializer.Deserialize<List<string>>(mainCostTypesJson) ?? new List<string>(); } catch { /* ignore */ } }


                var summary = await _costsDashboardRepository.GetCostsSummaryAsync(companyCode, categories, mainCostTypes, startDate, endDate);
                var mainCostSummaries = await _costsDashboardRepository.GetMainCostTypeSummaryAsync(companyCode, categories, mainCostTypes, startDate, endDate); // Fetch Main Type Summary
                var employeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(companyCode, categories, mainCostTypes, startDate, endDate);

                using var workbook = new XLWorkbook();

                string companyName = "ALL";
                if (companyCode != 0) { var company = await _companyRepository.GetCompanyByIdAsync(companyCode); if (company != null) companyName = company.CompanyName; }

                // --- UPDATED Sheet Creation ---
                CreateMainCostTypeSummarySheet(workbook.Worksheets.Add("Main Cost Type Summary"), summary, mainCostSummaries, startDate, endDate, categories, mainCostTypes, companyName);
                CreateEmployeeDetailsSheet(workbook.Worksheets.Add("Employee Details"), employeeCosts, startDate, endDate, categories, mainCostTypes, companyName); // Pass mainCostTypes

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                var fileName = $"CostsDashboard_{companyName.Replace(" ", "_")}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex) { /* ... error handling ... */ Console.WriteLine($"Error Exporting Excel: {ex.Message} \n {ex.StackTrace}"); TempData["ErrorMessage"] = $"Error exporting data: {ex.Message}"; return RedirectToAction("Index"); }
        }

        // --- NEW Helper for Main Cost Type Summary Sheet ---
        private void CreateMainCostTypeSummarySheet(IXLWorksheet worksheet, CostsSummary overallSummary, List<MainCostTypeSummary> mainCostSummaries, DateTime startDate, DateTime endDate, List<string> categories, List<string> mainCostTypes, string companyName)
        {
            int currentRow = 1;
            worksheet.Cell(currentRow, 1).Value = $"{companyName} - Main Cost Type Summary Report";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge().Style.Font.Bold = true; // Adjusted columns
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Period: {startDate:dd-MMM-yyyy} to {endDate:dd-MMM-yyyy}";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Categories: {(categories.Any() ? string.Join(", ", categories) : "ALL")}";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge(); currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Main Cost Types: {(mainCostTypes.Any() ? string.Join(", ", mainCostTypes) : "ALL")}"; // Updated label
            worksheet.Range(currentRow, 1, currentRow, 7).Merge(); currentRow++;
            currentRow++; // Blank

            // Headers
            int headerRow = currentRow;
            worksheet.Cell(headerRow, 1).Value = "Main Cost Type";
            worksheet.Cell(headerRow, 2).Value = "Present Cost";
            worksheet.Cell(headerRow, 3).Value = "Direct Cost";
            worksheet.Cell(headerRow, 4).Value = "Indirect Cost";
            worksheet.Cell(headerRow, 5).Value = "Absent Cost";
            worksheet.Cell(headerRow, 6).Value = "Leave Cost";
            worksheet.Cell(headerRow, 7).Value = "% of Total Present";
            var headerRange = worksheet.Range(headerRow, 1, headerRow, 7); // Adjusted columns
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data
            int dataStartRow = headerRow + 1;
            if (mainCostSummaries != null)
            {
                worksheet.Cell(dataStartRow, 1).InsertData(mainCostSummaries);
                worksheet.Range(dataStartRow, 2, dataStartRow + mainCostSummaries.Count - 1, 6).Style.NumberFormat.Format = "₹#,##0.00";
                worksheet.Range(dataStartRow, 7, dataStartRow + mainCostSummaries.Count - 1, 7).Style.NumberFormat.Format = "0.00%";
                // Adjust percentage value for Excel formatting
                for (int i = 0; i < mainCostSummaries.Count; i++)
                {
                    worksheet.Cell(dataStartRow + i, 7).Value = mainCostSummaries[i].PercentageOfTotalPresent / 100.0m;
                }
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(headerRow);
        }


        // --- UPDATED Employee Details Sheet Helper ---
        private void CreateEmployeeDetailsSheet(IXLWorksheet worksheet, List<EmployeeCostData> employeeCosts, DateTime startDate, DateTime endDate, List<string> categories, List<string> mainCostTypes, string companyName) // Changed param name
        {
            int currentRow = 1;
            worksheet.Cell(currentRow, 1).Value = $"{companyName} - Employee Cost Details";
            worksheet.Range(currentRow, 1, currentRow, 13).Merge().Style.Font.Bold = true; // Adjusted columns
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Period: {startDate:dd-MMM-yyyy} to {endDate:dd-MMM-yyyy}";
            worksheet.Range(currentRow, 1, currentRow, 13).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Categories: {(categories.Any() ? string.Join(", ", categories) : "ALL")}";
            worksheet.Range(currentRow, 1, currentRow, 13).Merge(); currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Main Cost Types: {(mainCostTypes.Any() ? string.Join(", ", mainCostTypes) : "ALL")}"; // Updated label
            worksheet.Range(currentRow, 1, currentRow, 13).Merge(); currentRow++;
            currentRow++; // Blank Row

            // Headers
            int headerRow = currentRow;
            worksheet.Cell(headerRow, 1).Value = "Employee Code";
            worksheet.Cell(headerRow, 2).Value = "Employee Name";
            worksheet.Cell(headerRow, 3).Value = "Department";
            worksheet.Cell(headerRow, 4).Value = "Category";
            worksheet.Cell(headerRow, 5).Value = "Cost Type";
            worksheet.Cell(headerRow, 6).Value = "Main Cost Type"; // Added column
            worksheet.Cell(headerRow, 7).Value = "Daily CTC";
            worksheet.Cell(headerRow, 8).Value = "Present Days";
            worksheet.Cell(headerRow, 9).Value = "Absent Days";
            worksheet.Cell(headerRow, 10).Value = "Leave Days";
            worksheet.Cell(headerRow, 11).Value = "Present Cost";
            worksheet.Cell(headerRow, 12).Value = "Absent Cost";
            worksheet.Cell(headerRow, 13).Value = "Leave Cost"; // Adjusted column index
            var headerRange = worksheet.Range(headerRow, 1, headerRow, 13); // Adjusted columns
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data
            int dataStartRow = headerRow + 1;
            if (employeeCosts != null)
            {
                var exportData = employeeCosts.Select(emp => new
                {
                    emp.EmployeeCode,
                    emp.EmployeeName,
                    emp.Department,
                    emp.Category,
                    emp.CostType,
                    emp.MainCostType, // Added MainCostType
                    emp.DailyCTC,
                    emp.PresentDays,
                    emp.AbsentDays,
                    emp.LeaveDays,
                    TotalCost = emp.TotalCost,
                    emp.AbsentCost,
                    emp.LeaveCost
                });
                worksheet.Cell(dataStartRow, 1).InsertData(exportData);

                // Formatting
                worksheet.Range(dataStartRow, 7, dataStartRow + employeeCosts.Count - 1, 7).Style.NumberFormat.Format = "₹#,##0.00"; // Daily CTC
                worksheet.Range(dataStartRow, 8, dataStartRow + employeeCosts.Count - 1, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; // Align Days Right
                worksheet.Range(dataStartRow, 11, dataStartRow + employeeCosts.Count - 1, 13).Style.NumberFormat.Format = "₹#,##0.00"; // Costs
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(headerRow);
        }


        private int GetRoleId(string roleName)
        {
            return roleName switch { "Admin" => 1, "HR" => 2, "User" => 3, "GM" => 4, _ => 3 };
        }
    }
}