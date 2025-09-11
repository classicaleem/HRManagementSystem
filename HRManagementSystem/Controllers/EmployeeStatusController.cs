using ClosedXML.Excel;
using HRManagementSystem.Data;
using HRManagementSystem.Models.EmployeeStatus;
using HRManagementSystem.Repositories.EmployeeStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace HRManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,HR,GM")]
    public class EmployeeStatusController : Controller
    {
        private readonly IEmployeeStatusRepository _employeeStatusRepository;
        private readonly ICompanyRepository _companyRepository;

        public EmployeeStatusController(
            IEmployeeStatusRepository employeeStatusRepository,
            ICompanyRepository companyRepository)
        {
            _employeeStatusRepository = employeeStatusRepository;
            _companyRepository = companyRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

            // Get companies for dropdown
            var companies = await _companyRepository.GetCompaniesByUserRoleAsync(GetRoleId(userRole), userCompanyCode);

            // Get filter options
            var departments = await _employeeStatusRepository.GetDepartmentsAsync(userCompanyCode);
            var categories = await _employeeStatusRepository.GetCategoriesAsync(userCompanyCode);
            var designations = await _employeeStatusRepository.GetDesignationsAsync(userCompanyCode);

            var model = new EmployeeStatusViewModel
            {
                Companies = new SelectList(companies, "CompanyCode", "CompanyName"),
                Departments = new SelectList(departments),
                Categories = new SelectList(categories),
                Designations = new SelectList(designations),
                SelectedCompanyCode = userCompanyCode
            };

            ViewBag.UserRole = userRole;
            ViewBag.UserCompanyCode = userCompanyCode;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetEmployeeData([FromBody] EmployeeStatusDataTableRequest request)
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

                var result = await _employeeStatusRepository.GetEmployeeDataAsync(request);

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
        public async Task<IActionResult> UpdateEmployeeStatus([FromBody] EmployeeStatusBulkUpdateRequest request)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Validate user permissions
                if (userRole != "Admin" && userRole != "HR" && userRole != "GM")
                {
                    return Json(new { success = false, message = "Insufficient permissions" });
                }

                // Validate request
                if (!request.EmployeeCodes.Any())
                {
                    return Json(new { success = false, message = "No employees selected" });
                }

                // Update employee status
                var result = await _employeeStatusRepository.UpdateEmployeeStatusAsync(request, User.Identity.Name);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Successfully updated {result.UpdatedCount} employee(s)",
                        updatedCount = result.UpdatedCount
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating employee status: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByCompany(int companyCode)
        {
            var departments = await _employeeStatusRepository.GetDepartmentsAsync(companyCode);
            return Json(departments);
        }

        [HttpGet]
        public async Task<IActionResult> GetDesignationsByDepartment(string department, int companyCode)
        {
            var designations = await _employeeStatusRepository.GetDesignationsByDepartmentAsync(department, companyCode);
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

        //export data

        [HttpPost]
        public async Task<IActionResult> ExportFilteredData([FromBody] EmployeeStatusDataTableRequest request)
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

                // Get all data directly (not through the action method)
                request.Start = 0;
                request.Length = int.MaxValue;
                var result = await _employeeStatusRepository.GetEmployeeDataForExportAsync(request);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Employee Status Report");

                    // Add title
                    worksheet.Cell(1, 1).Value = "Employee Status Management Report";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Range(1, 1, 1, 13).Merge();

                    // Add export date
                    worksheet.Cell(2, 1).Value = $"Export Date: {DateTime.Now:dd-MMM-yyyy HH:mm}";
                    worksheet.Range(2, 1, 2, 13).Merge();

                    // Add filters info
                    var row = 3;
                    if (request.CompanyCode > 0)
                    {
                        var companyName = result.Data.FirstOrDefault()?.CompanyName ?? "Unknown";
                        worksheet.Cell(row, 1).Value = $"Company: {companyName}";
                        worksheet.Range(row, 1, row, 13).Merge();
                        row++;
                    }
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
                    if (!string.IsNullOrEmpty(request.Designation))
                    {
                        worksheet.Cell(row, 1).Value = $"Designation: {request.Designation}";
                        worksheet.Range(row, 1, row, 13).Merge();
                        row++;
                    }
                    if (!string.IsNullOrEmpty(request.StatusFilter) && request.StatusFilter != "All")
                    {
                        worksheet.Cell(row, 1).Value = $"Status Filter: {request.StatusFilter}";
                        worksheet.Range(row, 1, row, 13).Merge();
                        row++;
                    }

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
                    worksheet.Cell(row, 9).Value = "In Time";
                    worksheet.Cell(row, 10).Value = "Attendance Status";
                    worksheet.Cell(row, 11).Value = "Long Absent";
                    worksheet.Cell(row, 12).Value = "Layoff";
                    worksheet.Cell(row, 13).Value = "Shift";

                    // Style headers
                    var headerRange = worksheet.Range(row, 1, row, 13);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                    headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                    // Add data
                    row++;
                    foreach (var emp in result.Data)
                    {
                        worksheet.Cell(row, 1).Value = emp.CompanyName ?? "";
                        worksheet.Cell(row, 2).Value = emp.EmployeeCode ?? "";
                        worksheet.Cell(row, 3).Value = emp.PunchNo ?? "";
                        worksheet.Cell(row, 4).Value = emp.EmployeeName ?? "";
                        worksheet.Cell(row, 5).Value = emp.Department ?? "";
                        worksheet.Cell(row, 6).Value = emp.Designation ?? "";
                        worksheet.Cell(row, 7).Value = emp.Category ?? "";
                        worksheet.Cell(row, 8).Value = emp.Section ?? "";

                        // Format InTime
                        var inTime = "No Punch";
                        if (!string.IsNullOrEmpty(emp.FirstPunchTime) && DateTime.TryParse(emp.FirstPunchTime, out DateTime punchTime))
                        {
                            inTime = punchTime.ToString("hh:mm tt");
                        }
                        worksheet.Cell(row, 9).Value = inTime;

                        worksheet.Cell(row, 10).Value = emp.AttendanceStatus ?? "Unknown";
                        worksheet.Cell(row, 11).Value = emp.LongAbsent ? "Yes" : "No";
                        worksheet.Cell(row, 12).Value = emp.Layoff ? "Yes" : "No";
                        worksheet.Cell(row, 13).Value = emp.Shift ?? "G";

                        // Apply conditional formatting for attendance status
                        var statusCell = worksheet.Cell(row, 10);
                        switch (emp.AttendanceStatus?.ToLower())
                        {
                            case "present":
                                statusCell.Style.Font.FontColor = XLColor.Green;
                                statusCell.Style.Font.Bold = true;
                                break;
                            case "absent":
                                statusCell.Style.Font.FontColor = XLColor.Red;
                                statusCell.Style.Font.Bold = true;
                                break;
                            case "leave":
                                statusCell.Style.Font.FontColor = XLColor.Orange;
                                break;
                            case "half day":
                                statusCell.Style.Font.FontColor = XLColor.Blue;
                                break;
                        }

                        // Apply conditional formatting for Long Absent
                        if (emp.LongAbsent)
                        {
                            worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.Orange;
                            worksheet.Cell(row, 11).Style.Font.Bold = true;
                        }

                        // Apply conditional formatting for Layoff
                        if (emp.Layoff)
                        {
                            worksheet.Cell(row, 12).Style.Font.FontColor = XLColor.Red;
                            worksheet.Cell(row, 12).Style.Font.Bold = true;
                        }

                        row++;
                    }

                    // Add summary
                    row += 2;
                    var totalEmployees = result.Data.Count;
                    var totalLongAbsent = result.Data.Count(x => x.LongAbsent);
                    var totalLayoff = result.Data.Count(x => x.Layoff);
                    var totalActive = result.Data.Count(x => !x.LongAbsent && !x.Layoff);
                    var totalPresent = result.Data.Count(x => x.AttendanceStatus?.ToLower() == "present");
                    var totalAbsent = result.Data.Count(x => x.AttendanceStatus?.ToLower() == "absent");

                    worksheet.Cell(row, 1).Value = "Summary";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                    row++;

                    worksheet.Cell(row, 1).Value = $"Total Employees: {totalEmployees}";
                    row++;

                    worksheet.Cell(row, 1).Value = $"Active Employees: {totalActive}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Green;
                    row++;

                    worksheet.Cell(row, 1).Value = $"Long Absent: {totalLongAbsent}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Orange;
                    row++;

                    worksheet.Cell(row, 1).Value = $"Layoff: {totalLayoff}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Red;
                    row++;

                    worksheet.Cell(row, 1).Value = $"Present Today: {totalPresent}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Green;
                    row++;

                    worksheet.Cell(row, 1).Value = $"Absent Today: {totalAbsent}";
                    worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Red;

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Generate file
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        var filenameSuffix = request.StatusFilter switch
                        {
                            "Active" => "_Active",
                            "LongAbsent" => "_LongAbsent",
                            "Layoff" => "_Layoff",
                            _ => "_All"
                        };

                        var fileName = $"EmployeeStatusReport{filenameSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error exporting data: {ex.Message}" });
            }
        }
    }
}