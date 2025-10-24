//using ClosedXML.Excel;
//using HRManagementSystem.Data;
//using HRManagementSystem.Models.CostsDashboard;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using System.Security.Claims;

//namespace HRManagementSystem.Controllers
//{
//    [Authorize(Roles = "Admin,HR,GM")]
//    public class CostsDashboardController : Controller
//    {
//        private readonly ICostsDashboardRepository _costsDashboardRepository;
//        private readonly ICompanyRepository _companyRepository;

//        public CostsDashboardController(
//            ICostsDashboardRepository costsDashboardRepository,
//            ICompanyRepository companyRepository)
//        {
//            _costsDashboardRepository = costsDashboardRepository;
//            _companyRepository = companyRepository;
//        }

//        /// <summary>
//        /// Main dashboard page
//        /// </summary>
//        public async Task<IActionResult> Index()
//        {
//            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
//            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

//            // Get companies for dropdown
//            var companies = await _companyRepository.GetCompaniesByUserRoleAsync(GetRoleId(userRole), userCompanyCode);

//            // Default to current month
//            var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
//            var endDate = DateTime.Now.Date;

//            // Get departments
//            var departments = await _costsDashboardRepository.GetDepartmentsByCompanyAsync(userCompanyCode);

//            var model = new CostsDashboardViewModel
//            {
//                SelectedCompanyCode = userCompanyCode,
//                SelectedDepartment = "ALL",
//                StartDate = startDate,
//                EndDate = endDate,
//                Companies = new SelectList(companies, "CompanyCode", "CompanyName"),
//                Departments = new SelectList(departments),
//                Summary = await _costsDashboardRepository.GetCostsSummaryAsync(userCompanyCode, "ALL", startDate, endDate),
//                DepartmentCosts = await _costsDashboardRepository.GetDepartmentCostsAsync(userCompanyCode, startDate, endDate),
//                EmployeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(userCompanyCode, "ALL", startDate, endDate)
//            };

//            return View(model);
//        }

//        /// <summary>
//        /// AJAX method to refresh dashboard data
//        /// </summary>
//        [HttpPost]
//        public async Task<IActionResult> RefreshData(int companyCode, string department, DateTime startDate, DateTime endDate)
//        {
//            try
//            {
//                var summary = await _costsDashboardRepository.GetCostsSummaryAsync(companyCode, department, startDate, endDate);
//                var departmentCosts = await _costsDashboardRepository.GetDepartmentCostsAsync(companyCode, startDate, endDate);
//                var employeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(companyCode, department, startDate, endDate);

//                return Json(new
//                {
//                    success = true,
//                    summary = summary,
//                    departmentCosts = departmentCosts,
//                    employeeCosts = employeeCosts
//                });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        /// <summary>
//        /// Get departments by company for dropdown
//        /// </summary>
//        [HttpGet]
//        public async Task<IActionResult> GetDepartmentsByCompany(int companyCode)
//        {
//            try
//            {
//                var departments = await _costsDashboardRepository.GetDepartmentsByCompanyAsync(companyCode);
//                return Json(departments);
//            }
//            catch (Exception ex)
//            {
//                return Json(new List<string>());
//            }
//        }

//        /// <summary>
//        /// Export employee costs to Excel with formatting
//        /// </summary>
//        [HttpPost]
//        public async Task<IActionResult> ExportToExcel(int companyCode, string department, DateTime startDate, DateTime endDate)
//        {
//            try
//            {
//                var employeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(companyCode, department, startDate, endDate);
//                var summary = await _costsDashboardRepository.GetCostsSummaryAsync(companyCode, department, startDate, endDate);

//                using var workbook = new XLWorkbook();
//                var worksheet = workbook.Worksheets.Add("Employee Costs");

//                // Title
//                worksheet.Cell(1, 1).Value = "Employee Costs Report";
//                worksheet.Range(1, 1, 1, 11).Merge();
//                worksheet.Cell(1, 1).Style.Font.Bold = true;
//                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
//                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#007bff");
//                worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;

//                // Period info
//                worksheet.Cell(2, 1).Value = $"Period: {startDate:dd-MMM-yyyy} to {endDate:dd-MMM-yyyy}";
//                worksheet.Range(2, 1, 2, 11).Merge();
//                worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                worksheet.Cell(2, 1).Style.Font.Bold = true;

//                // Summary
//                worksheet.Cell(3, 1).Value = "Summary:";
//                worksheet.Cell(3, 1).Style.Font.Bold = true;

//                worksheet.Cell(4, 1).Value = $"Total Cost: ₹{summary.TotalCost:N2}";
//                worksheet.Cell(4, 4).Value = $"Direct Cost: ₹{summary.DirectCost:N2} ({summary.DirectCostPercentage}%)";
//                worksheet.Cell(4, 8).Value = $"Indirect Cost: ₹{summary.IndirectCost:N2} ({summary.IndirectCostPercentage}%)";

//                // Headers
//                var headerRow = 6;
//                worksheet.Cell(headerRow, 1).Value = "Company";
//                worksheet.Cell(headerRow, 2).Value = "Employee Code";
//                worksheet.Cell(headerRow, 3).Value = "Punch No";
//                worksheet.Cell(headerRow, 4).Value = "Employee Name";
//                worksheet.Cell(headerRow, 5).Value = "Department";
//                worksheet.Cell(headerRow, 6).Value = "Cost Type";
//                worksheet.Cell(headerRow, 7).Value = "Designation";
//                worksheet.Cell(headerRow, 8).Value = "Category";
//                worksheet.Cell(headerRow, 9).Value = "Daily CTC";
//                worksheet.Cell(headerRow, 10).Value = "Present Days";
//                worksheet.Cell(headerRow, 11).Value = "Total Cost";

//                // Style headers
//                var headerRange = worksheet.Range(headerRow, 1, headerRow, 11);
//                headerRange.Style.Font.Bold = true;
//                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#28a745");
//                headerRange.Style.Font.FontColor = XLColor.White;
//                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

//                // Data
//                int row = headerRow + 1;
//                foreach (var emp in employeeCosts)
//                {
//                    worksheet.Cell(row, 1).Value = emp.CompanyName;
//                    worksheet.Cell(row, 2).Value = emp.EmployeeCode;
//                    worksheet.Cell(row, 3).Value = emp.PunchNo;
//                    worksheet.Cell(row, 4).Value = emp.EmployeeName;
//                    worksheet.Cell(row, 5).Value = emp.Department;
//                    worksheet.Cell(row, 6).Value = emp.CostType;
//                    worksheet.Cell(row, 7).Value = emp.Designation;
//                    worksheet.Cell(row, 8).Value = emp.Category;
//                    worksheet.Cell(row, 9).Value = emp.DailyCTC;
//                    worksheet.Cell(row, 10).Value = emp.PresentDays;
//                    worksheet.Cell(row, 11).Value = emp.TotalCost;

//                    // Format currency columns
//                    worksheet.Cell(row, 9).Style.NumberFormat.Format = "₹#,##0.00";
//                    worksheet.Cell(row, 11).Style.NumberFormat.Format = "₹#,##0.00";

//                    // Color code cost type
//                    if (emp.IsDirect)
//                    {
//                        worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightGreen;
//                    }
//                    else
//                    {
//                        worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightYellow;
//                    }

//                    // Add borders
//                    worksheet.Range(row, 1, row, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

//                    row++;
//                }

//                // Totals row
//                if (employeeCosts.Any())
//                {
//                    worksheet.Cell(row, 1).Value = "GRAND TOTAL";
//                    worksheet.Cell(row, 1).Style.Font.Bold = true;
//                    worksheet.Range(row, 1, row, 8).Merge();
//                    worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

//                    worksheet.Cell(row, 10).Value = employeeCosts.Sum(e => e.PresentDays);
//                    worksheet.Cell(row, 11).Value = employeeCosts.Sum(e => e.TotalCost);
//                    worksheet.Cell(row, 11).Style.NumberFormat.Format = "₹#,##0.00";

//                    var totalRange = worksheet.Range(row, 1, row, 11);
//                    totalRange.Style.Font.Bold = true;
//                    totalRange.Style.Fill.BackgroundColor = XLColor.LightGray;
//                    totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
//                }

//                // Auto-fit columns
//                worksheet.Columns().AdjustToContents();

//                // Set minimum column widths
//                worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 20);
//                worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 25);
//                worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 20);

//                using var stream = new MemoryStream();
//                workbook.SaveAs(stream);
//                var content = stream.ToArray();

//                var fileName = $"EmployeeCosts_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
//                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }

//        /// <summary>
//        /// View to manage department cost type configuration
//        /// </summary>
//        [Authorize(Roles = "Admin")]
//        public async Task<IActionResult> ManageDepartmentCostTypes()
//        {
//            var departmentCostTypes = await _costsDashboardRepository.GetAllDepartmentCostTypesAsync();
//            return View(departmentCostTypes);
//        }

//        /// <summary>
//        /// Update department cost type
//        /// </summary>
//        [HttpPost]
//        [Authorize(Roles = "Admin")]
//        public async Task<IActionResult> UpdateDepartmentCostType(string departmentName, string costType)
//        {
//            try
//            {
//                var result = await _costsDashboardRepository.UpdateDepartmentCostTypeAsync(departmentName, costType);

//                if (result)
//                {
//                    return Json(new { success = true, message = "Department cost type updated successfully" });
//                }
//                else
//                {
//                    return Json(new { success = false, message = "Failed to update department cost type" });
//                }
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        private int GetRoleId(string roleName)
//        {
//            return roleName switch
//            {
//                "Admin" => 1,
//                "HR" => 2,
//                "User" => 3,
//                "GM" => 4,
//                _ => 3
//            };
//        }
//    }
//}


using ClosedXML.Excel;
using HRManagementSystem.Data;
using HRManagementSystem.Models.CostsDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

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

        /// <summary>
        /// Main dashboard page
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

            // Get companies for dropdown
            var companies = await _companyRepository.GetCompaniesByUserRoleAsync(GetRoleId(userRole), userCompanyCode);

            // Default to current month
            var startDate = DateTime.Now.Date;// new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endDate = DateTime.Now.Date;

            // Get categories for multi-select
            var categories = await _costsDashboardRepository.GetCategoriesByCompanyAsync(userCompanyCode);

            // Default selected categories
            var defaultCategories = new List<string> { "WORKER", "GROWMORE", "Dailywages", "MALE-WORKER", "FTC", "DW-450" };
            var selectedCategories = categories.Where(c => defaultCategories.Contains(c)).ToList();
            if (!selectedCategories.Any() && categories.Any())
            {
                selectedCategories = categories.Take(6).ToList(); // If defaults don't exist, take first 6
            }

            var model = new CostsDashboardViewModel
            {
                SelectedCompanyCode = userCompanyCode,
                SelectedCategories = selectedCategories,
                StartDate = startDate,
                EndDate = endDate,
                Companies = new SelectList(companies, "CompanyCode", "CompanyName", userCompanyCode),
                Categories = new MultiSelectList(categories, selectedCategories),
                Summary = await _costsDashboardRepository.GetCostsSummaryAsync(userCompanyCode, selectedCategories, startDate, endDate),
                AttendanceSummary = await _costsDashboardRepository.GetAttendanceSummaryAsync(userCompanyCode, selectedCategories, startDate, endDate),
                DepartmentCosts = await _costsDashboardRepository.GetDepartmentCostsAsync(userCompanyCode, selectedCategories, startDate, endDate),
                // EmployeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(userCompanyCode, selectedCategories, startDate, endDate)
            };

            return View(model);
        }

        /// <summary>
        /// AJAX method to refresh dashboard data
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RefreshData(int companyCode, List<string> categories, DateTime startDate, DateTime endDate)
        {
            try
            {
                // If no categories selected, use defaults
                if (categories == null || !categories.Any())
                {
                    categories = new List<string> { "WORKER", "GROWMORE", "Dailywages", "MALE-WORKER", "FTC", "DW-450" };
                }

                var summary = await _costsDashboardRepository.GetCostsSummaryAsync(companyCode, categories, startDate, endDate);
                var attendanceSummary = await _costsDashboardRepository.GetAttendanceSummaryAsync(companyCode, categories, startDate, endDate);
                var departmentCosts = await _costsDashboardRepository.GetDepartmentCostsAsync(companyCode, categories, startDate, endDate);
                var employeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(companyCode, categories, startDate, endDate);

                return Json(new
                {
                    success = true,
                    summary = summary,
                    attendanceSummary = attendanceSummary,
                    departmentCosts = departmentCosts,
                    employeeCosts = employeeCosts
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesByCompany(int companyCode)
        {
            try
            {
                var categories = await _costsDashboardRepository.GetCategoriesByCompanyAsync(companyCode);

                // Return with default selection indicators
                var defaultCategories = new List<string> { "WORKER", "GROWMORE", "Dailywages", "MALE-WORKER", "FTC", "DW-450" };
                var categoriesWithDefaults = categories.Select(c => new
                {
                    value = c,
                    text = c,
                    selected = defaultCategories.Contains(c)
                }).ToList();

                return Json(categoriesWithDefaults);
            }
            catch (Exception ex)
            {
                return Json(new List<string>());
            }
        }


        [HttpPost]
        public async Task<IActionResult> ExportToExcel(int companyCode, string categoriesJson, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Parse categories from JSON
                var categories = string.IsNullOrEmpty(categoriesJson) ?
                    new List<string>() :
                    System.Text.Json.JsonSerializer.Deserialize<List<string>>(categoriesJson);

                if (!categories.Any())
                {
                    categories = new List<string> { "Worker", "Growmore", "Dailywages", "MALE-WORKER", "FTC", "DW-450" };
                }

                var employeeCosts = await _costsDashboardRepository.GetEmployeeCostsAsync(companyCode, categories, startDate, endDate);
                var summary = await _costsDashboardRepository.GetCostsSummaryAsync(companyCode, categories, startDate, endDate);
                var attendanceSummary = await _costsDashboardRepository.GetAttendanceSummaryAsync(companyCode, categories, startDate, endDate);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Attendance & Costs");

                // Title
                worksheet.Cell(1, 1).Value = "Attendance & Costs Report";
                worksheet.Range(1, 1, 1, 13).Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#007bff");
                worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;

                // Period info
                worksheet.Cell(2, 1).Value = $"Period: {startDate:dd-MMM-yyyy} to {endDate:dd-MMM-yyyy}";
                worksheet.Range(2, 1, 2, 13).Merge();
                worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, 1).Style.Font.Bold = true;

                // Categories
                worksheet.Cell(3, 1).Value = $"Categories: {string.Join(", ", categories)}";
                worksheet.Range(3, 1, 3, 13).Merge();
                worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Summary Section
                var summaryRow = 5;
                worksheet.Cell(summaryRow, 1).Value = "SUMMARY";
                worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
                worksheet.Cell(summaryRow, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Range(summaryRow, 1, summaryRow, 13).Style.Fill.BackgroundColor = XLColor.LightGray;

                summaryRow++;
                worksheet.Cell(summaryRow, 1).Value = "Total Cost:";
                worksheet.Cell(summaryRow, 2).Value = summary.TotalCost;
                worksheet.Cell(summaryRow, 2).Style.NumberFormat.Format = "₹#,##0.00";

                worksheet.Cell(summaryRow, 4).Value = "Direct Cost:";
                worksheet.Cell(summaryRow, 5).Value = summary.DirectCost;
                worksheet.Cell(summaryRow, 5).Style.NumberFormat.Format = "₹#,##0.00";

                worksheet.Cell(summaryRow, 7).Value = "Indirect Cost:";
                worksheet.Cell(summaryRow, 8).Value = summary.IndirectCost;
                worksheet.Cell(summaryRow, 8).Style.NumberFormat.Format = "₹#,##0.00";

                worksheet.Cell(summaryRow, 10).Value = "Absent Cost:";
                worksheet.Cell(summaryRow, 11).Value = summary.AbsentCost;
                worksheet.Cell(summaryRow, 11).Style.NumberFormat.Format = "₹#,##0.00";

                summaryRow++;
                worksheet.Cell(summaryRow, 1).Value = "Present Days:";
                worksheet.Cell(summaryRow, 2).Value = attendanceSummary.PresentCount;

                worksheet.Cell(summaryRow, 4).Value = "Absent Days:";
                worksheet.Cell(summaryRow, 5).Value = attendanceSummary.AbsentCount;

                worksheet.Cell(summaryRow, 7).Value = "Attendance %:";
                worksheet.Cell(summaryRow, 8).Value = $"{attendanceSummary.AttendancePercentage}%";

                worksheet.Cell(summaryRow, 10).Value = "Absenteeism %:";
                worksheet.Cell(summaryRow, 11).Value = $"{attendanceSummary.AbsenteeismRate}%";

                // Headers for employee data
                var headerRow = summaryRow + 3;
                worksheet.Cell(headerRow, 1).Value = "Company";
                worksheet.Cell(headerRow, 2).Value = "Employee Code";
                worksheet.Cell(headerRow, 3).Value = "Punch No";
                worksheet.Cell(headerRow, 4).Value = "Employee Name";
                worksheet.Cell(headerRow, 5).Value = "Department";
                worksheet.Cell(headerRow, 6).Value = "Cost Type";
                worksheet.Cell(headerRow, 7).Value = "Designation";
                worksheet.Cell(headerRow, 8).Value = "Category";
                worksheet.Cell(headerRow, 9).Value = "Daily CTC";
                worksheet.Cell(headerRow, 10).Value = "Present Days";
                worksheet.Cell(headerRow, 11).Value = "Absent Days";
                worksheet.Cell(headerRow, 12).Value = "Total Cost";
                worksheet.Cell(headerRow, 13).Value = "Absent Cost";

                // Style headers
                var headerRange = worksheet.Range(headerRow, 1, headerRow, 13);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#28a745");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // Data
                int row = headerRow + 1;
                foreach (var emp in employeeCosts)
                {
                    worksheet.Cell(row, 1).Value = emp.CompanyName;
                    worksheet.Cell(row, 2).Value = emp.EmployeeCode;
                    worksheet.Cell(row, 3).Value = emp.PunchNo;
                    worksheet.Cell(row, 4).Value = emp.EmployeeName;
                    worksheet.Cell(row, 5).Value = emp.Department;
                    worksheet.Cell(row, 6).Value = emp.CostType;
                    worksheet.Cell(row, 7).Value = emp.Designation;
                    worksheet.Cell(row, 8).Value = emp.Category;
                    worksheet.Cell(row, 9).Value = emp.DailyCTC;
                    worksheet.Cell(row, 10).Value = emp.PresentDays;
                    worksheet.Cell(row, 11).Value = emp.AbsentDays;
                    worksheet.Cell(row, 12).Value = emp.TotalCost;
                    worksheet.Cell(row, 13).Value = emp.AbsentCost;

                    // Format currency columns
                    worksheet.Cell(row, 9).Style.NumberFormat.Format = "₹#,##0.00";
                    worksheet.Cell(row, 12).Style.NumberFormat.Format = "₹#,##0.00";
                    worksheet.Cell(row, 13).Style.NumberFormat.Format = "₹#,##0.00";

                    // Color code cost type
                    if (emp.IsDirect)
                    {
                        worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }
                    else
                    {
                        worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }

                    // Highlight high absenteeism
                    if (emp.AbsentDays > 5)
                    {
                        worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }

                    // Add borders
                    worksheet.Range(row, 1, row, 13).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    row++;
                }

                // Totals row
                if (employeeCosts.Any())
                {
                    worksheet.Cell(row, 1).Value = "GRAND TOTAL";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 8).Merge();
                    worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(row, 10).Value = employeeCosts.Sum(e => e.PresentDays);
                    worksheet.Cell(row, 11).Value = employeeCosts.Sum(e => e.AbsentDays);
                    worksheet.Cell(row, 12).Value = employeeCosts.Sum(e => e.TotalCost);
                    worksheet.Cell(row, 13).Value = employeeCosts.Sum(e => e.AbsentCost);

                    worksheet.Cell(row, 12).Style.NumberFormat.Format = "₹#,##0.00";
                    worksheet.Cell(row, 13).Style.NumberFormat.Format = "₹#,##0.00";

                    var totalRange = worksheet.Range(row, 1, row, 13);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                    totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                var fileName = $"AttendanceCosts_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private int GetRoleId(string roleName)
        {
            return roleName switch
            {
                "Admin" => 1,
                "HR" => 2,
                "User" => 3,
                "GM" => 4,
                _ => 3
            };
        }
    }
}