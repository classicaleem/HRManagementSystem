using HRManagementSystem.Models;

namespace HRManagementSystem.Data
{
    public interface IAttendanceRepository
    {
        Task<bool> ProcessDailyAttendanceAsync(DateTime processDate);
        Task<AttendanceReportViewModel> GetDailyAttendanceReportAsync(DateTime reportDate, int companyCode);
        Task<List<CompanyAttendanceStats>> GetCompaniesWithAttendanceAsync(DateTime reportDate);
        Task<DepartmentAttendanceViewModel> GetDepartmentAttendanceReportAsync(DateTime reportDate, int companyCode, string department = "ALL");

    }
}