using HRManagementSystem.Data;
using HRManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace HRManagementSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IAttendanceProcessorService _attendanceProcessor;

        public HomeController(
            ICompanyRepository companyRepository,
            IAttendanceRepository attendanceRepository,
            IAttendanceProcessorService attendanceProcessor)
        {
            _companyRepository = companyRepository;
            _attendanceRepository = attendanceRepository;
            _attendanceProcessor = attendanceProcessor;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var companyCode) ? companyCode : 0;

            var companies = await _companyRepository.GetCompaniesByUserRoleAsync(GetRoleId(userRole), userCompanyCode);

            ViewBag.Companies = companies;
            ViewBag.UserRole = userRole;
            ViewBag.UserCompanyCode = userCompanyCode;

            return View();
        }

        public async Task<IActionResult> AttendanceReport(int companyCode = 0, DateTime? reportDate = null)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

            // Role-based access control
            if (userRole != "Admin" && companyCode != userCompanyCode)
            {
                companyCode = userCompanyCode;
            }

            var selectedDate = reportDate ?? DateTime.Today;
            var report = await _attendanceRepository.GetDailyAttendanceReportAsync(selectedDate, companyCode);

            // Get companies for dropdown (based on user role)
            var roleId = GetRoleId(userRole);
            report.Companies = await _companyRepository.GetCompaniesByUserRoleAsync(roleId, userCompanyCode);
            report.SelectedCompanyCode = companyCode;

            // ADD THIS: Create SelectList for dropdown
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

        [HttpGet]
        public async Task<IActionResult> GetAttendanceStats(int companyCode = 0)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

                // Role-based access control
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
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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

        #region 'department attendance report'
        // Add this method to your HomeController class

        public async Task<IActionResult> DepartmentAttendanceold(int companyCode = 0, DateTime? reportDate = null, string department = "ALL")
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

            // Role-based access control
            if (userRole != "Admin" && companyCode != userCompanyCode)
            {
                companyCode = userCompanyCode;
            }

            var selectedDate = reportDate ?? DateTime.Today;
            var report = await _attendanceRepository.GetDepartmentAttendanceReportAsync(selectedDate, companyCode, department);

            // Get companies for dropdown (based on user role)
            var roleId = GetRoleId(userRole);
            report.Companies = await _companyRepository.GetCompaniesByUserRoleAsync(roleId, userCompanyCode);
            report.SelectedCompanyCode = companyCode;
            report.SelectedDepartment = department;

            // Create SelectList for company dropdown
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

            // Create SelectList for department dropdown
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

            return View(report);
        }

        public async Task<IActionResult> DepartmentAttendance(int companyCode = 0, DateTime? reportDate = null, string department = "ALL")
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

            // Role-based access control
            if (userRole != "Admin" && companyCode != userCompanyCode)
            {
                companyCode = userCompanyCode;
            }

            var selectedDate = reportDate ?? DateTime.Today;
            var report = await _attendanceRepository.GetDepartmentAttendanceReportAsync(selectedDate, companyCode, department);

            // Get companies for dropdown (based on user role)
            var roleId = GetRoleId(userRole);
            report.Companies = await _companyRepository.GetCompaniesByUserRoleAsync(roleId, userCompanyCode);
            report.SelectedCompanyCode = companyCode;
            report.SelectedDepartment = department;

            // Create SelectList for company dropdown - only for Admin users
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

            // Create SelectList for department dropdown
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

            // Pass user role and company info to view
            ViewBag.UserRole = userRole;
            ViewBag.UserCompanyCode = userCompanyCode;

            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(string departmentName, int companyCode)
        {
            try
            {
                // Check if user has permission
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin" && userRole != "HR")
                {
                    return Json(new { success = false, message = "You don't have permission to add departments." });
                }

                // Add department logic here - you'll need to implement this in your repository
                // await _companyRepository.AddDepartmentAsync(departmentName, companyCode);

                return Json(new { success = true, message = "Department added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error adding department: {ex.Message}" });
            }
        }
        #endregion
    }
}