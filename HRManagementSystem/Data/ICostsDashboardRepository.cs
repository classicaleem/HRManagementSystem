//using HRManagementSystem.Models.CostsDashboard;

//namespace HRManagementSystem.Data
//{

//    public interface ICostsDashboardRepository
//    {
//        Task<CostsSummary> GetCostsSummaryAsync(int companyCode, string department, DateTime startDate, DateTime endDate);
//        Task<List<DepartmentCostData>> GetDepartmentCostsAsync(int companyCode, DateTime startDate, DateTime endDate);
//        Task<List<EmployeeCostData>> GetEmployeeCostsAsync(int companyCode, string department, DateTime startDate, DateTime endDate);
//        Task<List<string>> GetDepartmentsByCompanyAsync(int companyCode);
//        Task<List<DepartmentCostType>> GetAllDepartmentCostTypesAsync();
//        Task<bool> UpdateDepartmentCostTypeAsync(string departmentName, string costType);
//    }
//}

using HRManagementSystem.Models.CostsDashboard;

namespace HRManagementSystem.Data
{
    /// <summary>
    /// Interface for Costs Dashboard Repository
    /// </summary>
    public interface ICostsDashboardRepository
    {
        Task<CostsSummary> GetCostsSummaryAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate);
        Task<List<DepartmentCostData>> GetDepartmentCostsAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate);
        Task<List<EmployeeCostData>> GetEmployeeCostsAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate);
        Task<List<string>> GetDepartmentsByCompanyAsync(int companyCode);
        Task<List<string>> GetCategoriesByCompanyAsync(int companyCode);
        Task<List<DepartmentCostType>> GetAllDepartmentCostTypesAsync();
        Task<bool> UpdateDepartmentCostTypeAsync(string departmentName, string costType);
        Task<AttendanceSummary> GetAttendanceSummaryAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate);
    }
}