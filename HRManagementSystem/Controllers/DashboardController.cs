using Dapper;
using HRManagementSystem.Data;
using HRManagementSystem.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace HRManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILogger<DashboardController> _logger;
        private readonly string _connectionString;

        public DashboardController(
            ICompanyRepository companyRepository,
            IAttendanceRepository attendanceRepository,
            ILogger<DashboardController> logger,
            IConfiguration configuration)
        {
            _companyRepository = companyRepository;
            _attendanceRepository = attendanceRepository;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("NewAttendanceConnection");
        }

        public async Task<IActionResult> Index(int companyCode = 0)
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

                var roleId = GetRoleId(userRole);
                var companies = await _companyRepository.GetCompaniesByUserRoleAsync(roleId, userCompanyCode);

                // Get available categories
                var categories = await GetAvailableCategoriesAsync(companyCode);

                var model = new CategoryDashboardViewModel
                {
                    Companies = companies,
                    SelectedCompanyCode = companyCode,
                    UserRole = userRole,
                    UserCompanyCode = userCompanyCode,
                    AvailableCategories = categories,
                    // Default selected categories (excluding Staff)
                    SelectedCategories = categories.Where(c => c != "Staff").ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Dashboard Index");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCategoryDashboardData([FromBody] CategoryDashboardRequest request)
        {
            try
            {
                _logger.LogInformation($"GetCategoryDashboardData called - CompanyCode: {request.CompanyCode}, Categories: {string.Join(",", request.Categories)}");

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyCode = int.TryParse(User.FindFirst("CompanyCode")?.Value, out var userComp) ? userComp : 0;

                if (userRole != "Admin" && request.CompanyCode != userCompanyCode)
                {
                    request.CompanyCode = userCompanyCode;
                }

                var dashboardData = await GetCategoryBasedDataAsync(request.CompanyCode, request.Categories, request.Days);
                return Json(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoryDashboardData");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableCategories(int companyCode = 0)
        {
            try
            {
                var categories = await GetAvailableCategoriesAsync(companyCode);
                return Json(new { success = true, categories = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available categories");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<List<string>> GetAvailableCategoriesAsync(int companyCode)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                SELECT DISTINCT ISNULL(da.Category, 'Unknown') as Category
                FROM DailyAttendance da
                WHERE da.AttendanceDate = @Date
                AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                AND ISNULL(da.LongAbsent, 0) = 0
                AND da.Category IS NOT NULL
                AND da.Category != ''
                ORDER BY Category";

            var categories = await connection.QueryAsync<string>(sql, new
            {
                Date = DateTime.Today,
                CompanyCode = companyCode
            });

            return categories.ToList();
        }

        private async Task<object> GetCategoryBasedDataAsync(int companyCode, List<string> categories, int days)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                _logger.LogInformation($"Getting category data for {categories.Count} categories: {string.Join(", ", categories)}");

                // Get today's category-wise data with actual PerDayCTC - EXCLUDING LONGABSENT = 1
                var todaySql = @"
                    SELECT 
                        da.Category,
                        COUNT(*) as TotalEmployees,
                        COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentEmployees,
                        COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentEmployees,
                        SUM(ISNULL(da.PerDayCTC, 0)) as TotalBudgetedCost,
                        SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN ISNULL(da.PerDayCTC, 0) ELSE 0 END) as ActualCost,
                        AVG(ISNULL(da.PerDayCTC, 0)) as AvgCostPerEmployee
                    FROM DailyAttendance da
                    WHERE da.AttendanceDate = @TodayDate
                    AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                    AND ISNULL(da.LongAbsent, 0) = 0
                    AND da.Category IN @Categories
                    GROUP BY da.Category
                    ORDER BY da.Category";

                var todayData = await connection.QueryAsync(todaySql, new
                {
                    TodayDate = DateTime.Today,
                    CompanyCode = companyCode,
                    Categories = categories
                });

                _logger.LogInformation($"Found {todayData.Count()} category records for today");

                // Get trend data for selected categories - EXCLUDING LONGABSENT = 1
                var trendData = new List<object>();
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);

                    var dailySql = @"
                        SELECT 
                            COUNT(*) as TotalEmployees,
                            COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentEmployees,
                            COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentEmployees,
                            SUM(ISNULL(da.PerDayCTC, 0)) as TotalBudgetedCost,
                            SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN ISNULL(da.PerDayCTC, 0) ELSE 0 END) as ActualCost
                        FROM DailyAttendance da
                        WHERE da.AttendanceDate = @Date
                        AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                        AND ISNULL(da.LongAbsent, 0) = 0
                        AND da.Category IN @Categories";

                    var dayResult = await connection.QueryFirstOrDefaultAsync(dailySql, new
                    {
                        Date = date,
                        CompanyCode = companyCode,
                        Categories = categories
                    });

                    if (dayResult != null)
                    {
                        var totalEmployees = dayResult.TotalEmployees != null ? (int)dayResult.TotalEmployees : 0;
                        var presentEmployees = dayResult.PresentEmployees != null ? (int)dayResult.PresentEmployees : 0;
                        var absentEmployees = dayResult.AbsentEmployees != null ? (int)dayResult.AbsentEmployees : 0;
                        var budgetedCost = dayResult.TotalBudgetedCost != null ? (decimal)dayResult.TotalBudgetedCost : 0m;
                        var actualCost = dayResult.ActualCost != null ? (decimal)dayResult.ActualCost : 0m;

                        trendData.Add(new
                        {
                            date = date.ToString("yyyy-MM-dd"),
                            dayName = date.ToString("ddd"),
                            totalEmployees = totalEmployees,
                            presentEmployees = presentEmployees,
                            absentEmployees = absentEmployees,
                            budgetedCost = budgetedCost,
                            actualCost = actualCost,
                            costSaving = budgetedCost - actualCost,
                            attendancePercentage = totalEmployees > 0 ? Math.Round((double)presentEmployees / totalEmployees * 100, 2) : 0
                        });
                    }
                    else
                    {
                        // Add empty data for days with no records
                        trendData.Add(new
                        {
                            date = date.ToString("yyyy-MM-dd"),
                            dayName = date.ToString("ddd"),
                            totalEmployees = 0,
                            presentEmployees = 0,
                            absentEmployees = 0,
                            budgetedCost = 0m,
                            actualCost = 0m,
                            costSaving = 0m,
                            attendancePercentage = 0.0
                        });
                    }
                }

                _logger.LogInformation($"Generated {trendData.Count} days of trend data");

                // Calculate summary data
                var summary = CalculateSummary(todayData, categories);

                // Format category breakdown
                var categoryBreakdown = todayData.Select(d => new
                {
                    category = d.Category?.ToString() ?? "Unknown",
                    totalEmployees = d.TotalEmployees != null ? (int)d.TotalEmployees : 0,
                    presentEmployees = d.PresentEmployees != null ? (int)d.PresentEmployees : 0,
                    absentEmployees = d.AbsentEmployees != null ? (int)d.AbsentEmployees : 0,
                    budgetedCost = d.TotalBudgetedCost != null ? (decimal)d.TotalBudgetedCost : 0m,
                    actualCost = d.ActualCost != null ? (decimal)d.ActualCost : 0m,
                    costSaving = (d.TotalBudgetedCost != null ? (decimal)d.TotalBudgetedCost : 0m) -
                               (d.ActualCost != null ? (decimal)d.ActualCost : 0m),
                    avgCostPerEmployee = d.AvgCostPerEmployee != null ? (decimal)d.AvgCostPerEmployee : 0m,
                    attendancePercentage = (d.TotalEmployees != null ? (int)d.TotalEmployees : 0) > 0 ?
                        Math.Round((double)(d.PresentEmployees != null ? (int)d.PresentEmployees : 0) /
                                  (int)d.TotalEmployees * 100, 2) : 0
                }).ToList();

                _logger.LogInformation($"Dashboard data prepared successfully with {categoryBreakdown.Count} categories");

                return new
                {
                    success = true,
                    summary = summary,
                    trends = trendData,
                    categoryBreakdown = categoryBreakdown,
                    selectedCategories = categories,
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoryBasedDataAsync");
                throw;
            }
        }

        private object CalculateSummary(dynamic todayData, List<string> categories)
        {
            int totalEmployees = 0;
            int presentEmployees = 0;
            int absentEmployees = 0;
            decimal totalBudgetedCost = 0;
            decimal totalActualCost = 0;

            foreach (var item in todayData)
            {
                totalEmployees += item.TotalEmployees != null ? (int)item.TotalEmployees : 0;
                presentEmployees += item.PresentEmployees != null ? (int)item.PresentEmployees : 0;
                absentEmployees += item.AbsentEmployees != null ? (int)item.AbsentEmployees : 0;
                totalBudgetedCost += item.TotalBudgetedCost != null ? (decimal)item.TotalBudgetedCost : 0m;
                totalActualCost += item.ActualCost != null ? (decimal)item.ActualCost : 0m;
            }

            var costSaving = totalBudgetedCost - totalActualCost;
            var attendancePercentage = totalEmployees > 0 ? Math.Round((double)presentEmployees / totalEmployees * 100, 2) : 0;
            var costEfficiency = totalBudgetedCost > 0 ? Math.Round((double)costSaving / (double)totalBudgetedCost * 100, 2) : 0;

            return new
            {
                totalEmployees = totalEmployees,
                presentEmployees = presentEmployees,
                absentEmployees = absentEmployees,
                attendancePercentage = attendancePercentage,
                totalBudgetedCost = totalBudgetedCost,
                totalActualCost = totalActualCost,
                costSaving = costSaving,
                costEfficiency = costEfficiency,
                selectedCategoriesCount = categories.Count,
                avgCostPerEmployee = totalEmployees > 0 ? Math.Round(totalBudgetedCost / totalEmployees, 2) : 0
            };
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