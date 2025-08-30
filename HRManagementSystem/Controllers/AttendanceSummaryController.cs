using ClosedXML.Excel;
using HRManagementSystem.Data;
using HRManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Security.Claims;

namespace HRManagementSystem.Controllers
{
    [Authorize]
    public class AttendanceSummaryController : Controller
    {
        private readonly IAttendanceSummaryRepository _attendanceSummaryRepository;
        private readonly ICompanyRepository _companyRepository;

        public AttendanceSummaryController(
            IAttendanceSummaryRepository attendanceSummaryRepository,
            ICompanyRepository companyRepository)
        {
            _attendanceSummaryRepository = attendanceSummaryRepository;
            _companyRepository = companyRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

            // Get companies for dropdown
            var companies = await _companyRepository.GetCompaniesByUserRoleAsync(GetRoleId(userRole), userCompanyCode);

            // Get filter options - showing all employees by default
            var departments = await _attendanceSummaryRepository.GetDepartmentsAsync(userCompanyCode);
            var categories = await _attendanceSummaryRepository.GetCategoriesAsync(userCompanyCode);
            var designations = await _attendanceSummaryRepository.GetDesignationsAsync(userCompanyCode);

            var model = new AttendanceSummaryViewModel
            {
                FromDate = DateTime.Today,
                ToDate = DateTime.Today,
                Companies = new SelectList(companies, "CompanyCode", "CompanyName"),
                Departments = new SelectList(departments),
                Categories = new SelectList(categories),
                Designations = new SelectList(designations),
                AttendanceStatuses = new SelectList(new[] { "All", "Present", "Absent" }),
                LongAbsentOptions = new SelectList(new[] {
                    new { Value = "All", Text = "All Employees" },
                    new { Value = "ExcludeLongAbsent", Text = "Exclude Long Absent" },
                    new { Value = "OnlyLongAbsent", Text = "Only Long Absent" }
                }, "Value", "Text"),
                SelectedCompanyCode = userCompanyCode,
                SelectedAttendanceStatus = "All",
                SelectedLongAbsentOption = "All"
            };

            ViewBag.UserRole = userRole;
            ViewBag.UserCompanyCode = userCompanyCode;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetAttendanceData([FromBody] DataTableRequest request)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

                // Apply role-based filtering
                if (userRole != "Admin" && request.CompanyCode != userCompanyCode)
                {
                    request.CompanyCode = userCompanyCode;
                }

                var result = await _attendanceSummaryRepository.GetAttendanceSummaryAsync(request);

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = result.TotalRecords,
                    recordsFiltered = result.FilteredRecords,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = $"Error loading data: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromBody] AttendanceExportRequest request)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

                // Apply role-based filtering
                if (userRole != "Admin" && request.CompanyCode != userCompanyCode)
                {
                    request.CompanyCode = userCompanyCode;
                }

                var data = await _attendanceSummaryRepository.GetAttendanceForExportAsync(request);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Attendance Summary");

                    // Add title based on long absent filter
                    var titleSuffix = request.LongAbsentOption switch
                    {
                        "ExcludeLongAbsent" => " - Active Employees Only",
                        "OnlyLongAbsent" => " - Long Absent Employees Only",
                        _ => " - All Employees"
                    };
                    worksheet.Cell(1, 1).Value = $"Attendance Summary Report{titleSuffix}";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Range(1, 1, 1, 13).Merge();

                    // Add date range
                    worksheet.Cell(2, 1).Value = $"Period: {request.FromDate:dd-MMM-yyyy} to {request.ToDate:dd-MMM-yyyy}";
                    worksheet.Range(2, 1, 2, 13).Merge();

                    // Add filters info
                    var row = 3;
                    if (!string.IsNullOrEmpty(request.Department))
                    {
                        worksheet.Cell(row, 1).Value = $"Department: {request.Department}";
                        worksheet.Range(row, 1, row, 13).Merge();
                        row++;
                    }
                    if (!string.IsNullOrEmpty(request.Category))
                    {
                        worksheet.Cell(row, 1).Value = $"Category: {request.Category}";
                        worksheet.Range(row, 1, row, 13).Merge();
                        row++;
                    }

                    // Add long absent filter info
                    var longAbsentText = request.LongAbsentOption switch
                    {
                        "ExcludeLongAbsent" => "Excluding Long Absent Employees",
                        "OnlyLongAbsent" => "Only Long Absent Employees",
                        _ => "All Employees (Including Long Absent)"
                    };
                    worksheet.Cell(row, 1).Value = $"Employee Filter: {longAbsentText}";
                    worksheet.Range(row, 1, row, 13).Merge();
                    row++;

                    // Add headers
                    row += 2;
                    worksheet.Cell(row, 1).Value = "Company";
                    worksheet.Cell(row, 2).Value = "Employee Code";
                    worksheet.Cell(row, 3).Value = "Punch No";
                    worksheet.Cell(row, 4).Value = "Employee Name";
                    worksheet.Cell(row, 5).Value = "Department";
                    worksheet.Cell(row, 6).Value = "Designation";
                    worksheet.Cell(row, 7).Value = "Category";
                    worksheet.Cell(row, 8).Value = "Section";
                    worksheet.Cell(row, 9).Value = "Attendance Date";
                    worksheet.Cell(row, 10).Value = "First Punch Time";
                    worksheet.Cell(row, 11).Value = "Status";
                    worksheet.Cell(row, 12).Value = "Per Day CTC";
                    worksheet.Cell(row, 13).Value = "Long Absent";

                    // Style headers
                    var headerRange = worksheet.Range(row, 1, row, 13);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                    headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                    // Add data
                    row++;
                    foreach (var item in data)
                    {
                        worksheet.Cell(row, 1).Value = item.CompanyName;
                        worksheet.Cell(row, 2).Value = item.EmployeeCode;
                        worksheet.Cell(row, 3).Value = item.PunchNo;
                        worksheet.Cell(row, 4).Value = item.EmployeeName;
                        worksheet.Cell(row, 5).Value = item.Department;
                        worksheet.Cell(row, 6).Value = item.Designation;
                        worksheet.Cell(row, 7).Value = item.Category;
                        worksheet.Cell(row, 8).Value = item.Section;
                        worksheet.Cell(row, 9).Value = item.AttendanceDate.ToString("dd-MMM-yyyy");
                        worksheet.Cell(row, 10).Value = item.FirstPunchTime?.ToString(@"hh\:mm") ?? "-";
                        worksheet.Cell(row, 11).Value = item.AttendanceStatus;
                        worksheet.Cell(row, 12).Value = item.PerDayCTC;
                        worksheet.Cell(row, 13).Value = item.LongAbsent ? "Yes" : "No";

                        // Apply conditional formatting for status
                        if (item.AttendanceStatus == "Present")
                        {
                            worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.Green;
                        }
                        else if (item.AttendanceStatus == "Absent")
                        {
                            worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.Red;
                        }

                        // Apply conditional formatting for long absent
                        if (item.LongAbsent)
                        {
                            worksheet.Cell(row, 13).Style.Font.FontColor = XLColor.Orange;
                            worksheet.Cell(row, 13).Style.Font.Bold = true;
                        }

                        row++;
                    }

                    // Add summary
                    row += 2;
                    var totalPresent = data.Count(x => x.AttendanceStatus == "Present");
                    var totalAbsent = data.Count(x => x.AttendanceStatus == "Absent");
                    var totalEmployees = data.Select(x => x.EmployeeCode).Distinct().Count();
                    var totalLongAbsent = data.Count(x => x.LongAbsent);
                    var totalActive = data.Count(x => !x.LongAbsent);

                    worksheet.Cell(row, 1).Value = "Summary";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    row++;
                    worksheet.Cell(row, 1).Value = $"Total Employees: {totalEmployees}";
                    row++;
                    worksheet.Cell(row, 1).Value = $"Active Employees: {totalActive}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Green;
                    row++;
                    worksheet.Cell(row, 1).Value = $"Long Absent Employees: {totalLongAbsent}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Orange;
                    row++;
                    worksheet.Cell(row, 1).Value = $"Total Present: {totalPresent}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Green;
                    row++;
                    worksheet.Cell(row, 1).Value = $"Total Absent: {totalAbsent}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Red;

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Generate file
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        var filenameSuffix = request.LongAbsentOption switch
                        {
                            "ExcludeLongAbsent" => "_ActiveOnly",
                            "OnlyLongAbsent" => "_LongAbsentOnly",
                            _ => "_All"
                        };
                        var fileName = $"AttendanceSummary{filenameSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error exporting data: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByCompany(int companyCode)
        {
            var departments = await _attendanceSummaryRepository.GetDepartmentsAsync(companyCode);
            return Json(departments);
        }

        [HttpGet]
        public async Task<IActionResult> GetDesignationsByDepartment(string department, int companyCode)
        {
            var designations = await _attendanceSummaryRepository.GetDesignationsByDepartmentAsync(department, companyCode);
            return Json(designations);
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