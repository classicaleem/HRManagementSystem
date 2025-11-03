using HRManagementSystem.Models.CostsDashboard;

namespace HRManagementSystem.Data
{
    public interface ICostsDashboardRepository
    {
        // Overall Summary based on selected MainCostTypes
        Task<CostsSummary> GetCostsSummaryAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate);

        // Attendance Summary based on selected MainCostTypes
        Task<AttendanceSummary> GetAttendanceSummaryAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate);

        // NEW: Summary grouped by MainCostType for the main chart
        Task<List<MainCostTypeSummary>> GetMainCostTypeSummaryAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate);

        // Employee details, filtered by MainCostTypes
        Task<List<EmployeeCostData>> GetEmployeeCostsAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate);

        // Filters
        Task<List<string>> GetDepartmentsByCompanyAsync(int companyCode); // Keep if needed elsewhere, maybe remove if only for dashboard
        Task<List<string>> GetCategoriesByCompanyAsync(int companyCode);
        Task<List<string>> GetMainCostTypesByCompanyAsync(int companyCode); // NEW

        // Config
        Task<List<DepartmentCostType>> GetAllDepartmentCostTypesAsync();
        Task<bool> UpdateDepartmentCostTypeAsync(string departmentName, string costType, string mainCostType); // Added mainCostType
    }
}