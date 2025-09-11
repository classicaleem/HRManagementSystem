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
    }
}