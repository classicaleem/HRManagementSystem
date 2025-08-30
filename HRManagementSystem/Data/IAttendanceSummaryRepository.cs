using HRManagementSystem.Models;

namespace HRManagementSystem.Data
{
    public interface IAttendanceSummaryRepository
    {
        Task<DataTableResponse<AttendanceSummaryData>> GetAttendanceSummaryAsync(DataTableRequest request);
        Task<List<AttendanceSummaryData>> GetAttendanceForExportAsync(AttendanceExportRequest request);
        Task<List<string>> GetDepartmentsAsync(int companyCode);
        Task<List<string>> GetCategoriesAsync(int companyCode);
        Task<List<string>> GetDesignationsAsync(int companyCode);
        Task<List<string>> GetDesignationsByDepartmentAsync(string department, int companyCode);
    }
}
