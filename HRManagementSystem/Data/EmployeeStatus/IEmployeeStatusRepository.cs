using HRManagementSystem.Models.EmployeeStatus;

namespace HRManagementSystem.Repositories.EmployeeStatus
{
    public interface IEmployeeStatusRepository
    {
        Task<EmployeeStatusDataTableResponse<EmployeeStatusData>> GetEmployeeDataAsync(EmployeeStatusDataTableRequest request);
        Task<EmployeeStatusUpdateResult> UpdateEmployeeStatusAsync(EmployeeStatusBulkUpdateRequest request, string updatedBy);
        Task<List<string>> GetDepartmentsAsync(int companyCode);
        Task<List<string>> GetCategoriesAsync(int companyCode);
        Task<List<string>> GetDesignationsAsync(int companyCode);
        Task<List<string>> GetDesignationsByDepartmentAsync(string department, int companyCode);

        Task<EmployeeStatusDataTableResponse<EmployeeStatusData>> GetEmployeeDataForExportAsync(EmployeeStatusDataTableRequest request);
    }
}