using Dapper;
using HRManagementSystem.Data;
using HRManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace HRManagementSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IAttendanceProcessorService _attendanceProcessor;
        private readonly IConfiguration _configuration;

        public HomeController(
            ICompanyRepository companyRepository,
            IAttendanceRepository attendanceRepository,
            IAttendanceProcessorService attendanceProcessor,
            IConfiguration configuration)
        {
            _companyRepository = companyRepository;
            _attendanceRepository = attendanceRepository;
            _attendanceProcessor = attendanceProcessor;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

            var companies = await _companyRepository.GetCompaniesByUserRoleAsync(GetRoleId(userRole), userCompanyCode);

            ViewBag.Companies = companies;
            ViewBag.UserRole = userRole;
            ViewBag.UserCompanyCode = userCompanyCode;
            ViewBag.IsSuperAdmin = userRole == "Admin";

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCompaniesFromAttendance()
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("NewAttendanceConnection"));

                var sql = @"
                    SELECT DISTINCT da.CompanyCode, c.CompanyName
                    FROM DailyAttendance da
                    INNER JOIN Companies c ON da.CompanyCode = c.CompanyCode
                    WHERE da.AttendanceDate = CAST(GETDATE() AS DATE)
                    ORDER BY c.CompanyName";

                var companies = await connection.QueryAsync(sql);
                var result = companies.Select(c => new
                {
                    companyCode = (int)c.CompanyCode,
                    companyName = (string)c.CompanyName
                }).ToList();

                Console.WriteLine($"GetCompaniesFromAttendance - Count: {result.Count}");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCompaniesFromAttendance: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsFromAttendance(int companyCode = 0)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("NewAttendanceConnection"));

                var sql = @"
                    SELECT DISTINCT LTRIM(RTRIM(Department)) as Department
                    FROM DailyAttendance
                    WHERE Department IS NOT NULL 
                    AND LTRIM(RTRIM(Department)) != ''
                    AND AttendanceDate = CAST(GETDATE() AS DATE)";

                if (companyCode > 0)
                {
                    sql += " AND CompanyCode = @CompanyCode";
                }

                sql += " ORDER BY Department";

                var departments = await connection.QueryAsync<string>(sql, new { CompanyCode = companyCode });
                var deptList = departments.ToList();

                Console.WriteLine($"GetDepartmentsFromAttendance - CompanyCode: {companyCode}, Count: {deptList.Count}");

                return Json(deptList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartmentsFromAttendance: {ex.Message}");
                return Json(new List<string>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesFromAttendance(int companyCode = 0)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("NewAttendanceConnection"));

                var sql = @"
                    SELECT DISTINCT LTRIM(RTRIM(Category)) as Category
                    FROM DailyAttendance
                    WHERE Category IS NOT NULL 
                    AND LTRIM(RTRIM(Category)) != ''
                    AND AttendanceDate = CAST(GETDATE() AS DATE)";

                if (companyCode > 0)
                {
                    sql += " AND CompanyCode = @CompanyCode";
                }

                sql += " ORDER BY Category";

                var categories = await connection.QueryAsync<string>(sql, new { CompanyCode = companyCode });
                var catList = categories.ToList();

                Console.WriteLine($"GetCategoriesFromAttendance - CompanyCode: {companyCode}, Count: {catList.Count}");

                return Json(catList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCategoriesFromAttendance: {ex.Message}");
                return Json(new List<string>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCompaniesStats(int companyCode = 0, string department = "", string category = "")
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Unauthorized access" });
                }

                Console.WriteLine($"GetAllCompaniesStats - CompanyCode: {companyCode}, Department: '{department}', Category: '{category}'");

                using var connection = new SqlConnection(_configuration.GetConnectionString("NewAttendanceConnection"));

                var sql = @"
                    SELECT 
                        c.CompanyCode,
                        c.CompanyName,
                        COUNT(DISTINCT da.EmployeeCode) as TotalEmployees,
                        COUNT(DISTINCT CASE WHEN da.AttendanceStatus = 'Present' AND ISNULL(da.Layoff, 0) = 0 THEN da.EmployeeCode END) as PresentEmployees,
                        COUNT(DISTINCT CASE WHEN da.AttendanceStatus = 'Absent' AND ISNULL(da.Layoff, 0) = 0 THEN da.EmployeeCode END) as AbsentEmployees,
                        COUNT(DISTINCT CASE WHEN ISNULL(da.Layoff, 0) = 1 THEN da.EmployeeCode END) as LayoffEmployees,
                        CASE 
                            WHEN (COUNT(DISTINCT da.EmployeeCode) - COUNT(DISTINCT CASE WHEN ISNULL(da.Layoff, 0) = 1 THEN da.EmployeeCode END)) > 0 
                            THEN CAST(ROUND((COUNT(DISTINCT CASE WHEN da.AttendanceStatus = 'Present' THEN da.EmployeeCode END) * 100.0) / 
                                 (COUNT(DISTINCT da.EmployeeCode) - COUNT(DISTINCT CASE WHEN ISNULL(da.Layoff, 0) = 1 THEN da.EmployeeCode END)), 0) AS INT)
                            ELSE 0 
                        END as AttendancePercentage
                    FROM Companies c
                    INNER JOIN DailyAttendance da ON c.CompanyCode = da.CompanyCode
                    WHERE da.AttendanceDate = CAST(GETDATE() AS DATE)
                    AND ISNULL(da.LongAbsent, 0) = 0";

                var parameters = new DynamicParameters();

                if (companyCode > 0)
                {
                    sql += " AND da.CompanyCode = @CompanyCode";
                    parameters.Add("CompanyCode", companyCode);
                }

                if (!string.IsNullOrEmpty(department))
                {
                    sql += " AND LTRIM(RTRIM(da.Department)) = @Department";
                    parameters.Add("Department", department.Trim());
                }

                if (!string.IsNullOrEmpty(category))
                {
                    sql += " AND LTRIM(RTRIM(da.Category)) = @Category";
                    parameters.Add("Category", category.Trim());
                }

                sql += " GROUP BY c.CompanyCode, c.CompanyName ORDER BY c.CompanyName";

                Console.WriteLine($"Executing SQL with filters - Company: {companyCode}, Dept: '{department}', Cat: '{category}'");

                var companies = await connection.QueryAsync(sql, parameters);

                var companiesStats = companies.Select(c => new
                {
                    companyCode = (int)c.CompanyCode,
                    companyName = (string)c.CompanyName,
                    totalEmployees = (int)c.TotalEmployees,
                    presentEmployees = (int)c.PresentEmployees,
                    absentEmployees = (int)c.AbsentEmployees,
                    layoffEmployees = (int)c.LayoffEmployees,
                    attendancePercentage = (int)c.AttendancePercentage
                }).ToList();

                Console.WriteLine($"Returning {companiesStats.Count} companies");

                return Json(new
                {
                    success = true,
                    companies = companiesStats,
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllCompaniesStats: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceStats(int companyCode = 0)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

                if (userRole != "Admin" && companyCode != userCompanyCode)
                {
                    companyCode = userCompanyCode;
                }

                var stats = await _attendanceProcessor.GetAttendanceStatsAsync(companyCode);
                return Json(new
                {
                    success = true,
                    totalEmployees = stats.TotalEmployees,
                    presentEmployees = stats.PresentEmployees,
                    absentEmployees = stats.AbsentEmployees,
                    layoffEmployees = stats.LayoffEmployees,
                    attendancePercentage = stats.AttendancePercentage,
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftAttendanceStats(int companyCode = 0)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

                if (userRole != "Admin" && companyCode != userCompanyCode)
                {
                    companyCode = userCompanyCode;
                }

                var shiftStats = await _attendanceRepository.GetShiftAttendanceStatsWithLayoffAsync(companyCode);
                return Json(new
                {
                    success = true,
                    shifts = shiftStats,
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> AttendanceReport(int companyCode = 0, DateTime? reportDate = null)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

            if (userRole != "Admin" && companyCode != userCompanyCode)
            {
                companyCode = userCompanyCode;
            }

            var selectedDate = reportDate ?? DateTime.Today;
            var report = await _attendanceRepository.GetDailyAttendanceReportWithLayoffAsync(selectedDate, companyCode);

            var roleId = GetRoleId(userRole);
            report.Companies = await _companyRepository.GetCompaniesByUserRoleAsync(roleId, userCompanyCode);
            report.SelectedCompanyCode = companyCode;

            var companyOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "All Companies" }
            };
            companyOptions.AddRange(report.Companies.Select(c => new SelectListItem
            {
                Value = c.CompanyCode.ToString(),
                Text = c.CompanyName,
                Selected = c.CompanyCode == companyCode
            }));

            report.CompanySelectList = new SelectList(companyOptions, "Value", "Text", companyCode.ToString());

            return View(report);
        }

        public async Task<IActionResult> DepartmentAttendance(int companyCode = 0, DateTime? reportDate = null, string department = "ALL")
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

            if (userRole != "Admin" && companyCode != userCompanyCode)
            {
                companyCode = userCompanyCode;
            }

            var selectedDate = reportDate ?? DateTime.Today;
            var report = await _attendanceRepository.GetDepartmentAttendanceReportWithLayoffAsync(selectedDate, companyCode, department);

            var roleId = GetRoleId(userRole);
            report.Companies = await _companyRepository.GetCompaniesByUserRoleAsync(roleId, userCompanyCode);
            report.SelectedCompanyCode = companyCode;
            report.SelectedDepartment = department;

            var companyOptions = new List<SelectListItem>();

            if (userRole == "Admin")
            {
                companyOptions.Add(new SelectListItem { Value = "0", Text = "All Companies" });
                companyOptions.AddRange(report.Companies.Select(c => new SelectListItem
                {
                    Value = c.CompanyCode.ToString(),
                    Text = c.CompanyName,
                    Selected = c.CompanyCode == companyCode
                }));
            }

            report.CompanySelectList = new SelectList(companyOptions, "Value", "Text", companyCode.ToString());

            var departmentOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "ALL", Text = "All Departments" }
            };
            departmentOptions.AddRange(report.AvailableDepartments.Select(d => new SelectListItem
            {
                Value = d,
                Text = d,
                Selected = d == department
            }));

            report.DepartmentSelectList = new SelectList(departmentOptions, "Value", "Text", department);

            ViewBag.UserRole = userRole;
            ViewBag.UserCompanyCode = userCompanyCode;

            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshAttendance()
        {
            try
            {
                await _attendanceProcessor.ProcessTodayAttendanceAsync();
                return Json(new { success = true, message = "Attendance data refreshed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error refreshing attendance: {ex.Message}" });
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