using HRManagementSystem.Models;

namespace HRManagementSystem.Services
{
    public interface IAttendanceProcessorService
    {
        Task ProcessTodayAttendanceAsync();
        Task<AttendanceReportViewModel> GetAttendanceStatsAsync(int companyCode);
    }
}