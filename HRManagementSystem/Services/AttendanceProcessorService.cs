using HRManagementSystem.Data;
using HRManagementSystem.Models;

namespace HRManagementSystem.Services
{
    public class AttendanceProcessorService : IAttendanceProcessorService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IAttendanceNotificationService _notificationService;
        private readonly ILogger<AttendanceProcessorService> _logger;

        public AttendanceProcessorService(
            IAttendanceRepository attendanceRepository,
            IAttendanceNotificationService notificationService,
            ILogger<AttendanceProcessorService> logger)
        {
            _attendanceRepository = attendanceRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessTodayAttendanceAsync()
        {
            try
            {
                // Notify that processing has started
                await _notificationService.NotifyAttendanceProcessing(true);

                var today = DateTime.Today;
                var result = await _attendanceRepository.ProcessDailyAttendanceAsync(today);

                if (result)
                {
                    _logger.LogInformation($"Successfully processed attendance for {today:yyyy-MM-dd}");

                    // Get updated stats for all companies and notify
                    var companies = await _attendanceRepository.GetCompaniesWithAttendanceAsync(today);

                    foreach (var company in companies)
                    {
                        await _notificationService.NotifyAttendanceUpdated(
                            company.CompanyCode,
                            company.TotalEmployees,
                            company.PresentEmployees,
                            company.AbsentEmployees);
                    }
                }
                else
                {
                    _logger.LogError($"Failed to process attendance for {today:yyyy-MM-dd}");
                    await _notificationService.NotifyError("Failed to process attendance data");
                }

                // Notify that processing has completed
                await _notificationService.NotifyAttendanceProcessing(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessTodayAttendanceAsync");
                await _notificationService.NotifyError($"Error processing attendance: {ex.Message}");
                await _notificationService.NotifyAttendanceProcessing(false);
            }
        }

        public async Task<AttendanceReportViewModel> GetAttendanceStatsAsync(int companyCode)
        {
            return await _attendanceRepository.GetDailyAttendanceReportAsync(DateTime.Today, companyCode);
        }
    }
}