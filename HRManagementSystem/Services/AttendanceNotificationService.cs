using HRManagementSystem.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HRManagementSystem.Services
{
    public class AttendanceNotificationService : IAttendanceNotificationService
    {
        private readonly IHubContext<AttendanceHub> _hubContext;

        public AttendanceNotificationService(IHubContext<AttendanceHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyAttendanceUpdated(int companyCode, int totalEmployees, int presentEmployees, int absentEmployees)
        {
            var attendanceData = new
            {
                CompanyCode = companyCode,
                TotalEmployees = totalEmployees,
                PresentEmployees = presentEmployees,
                AbsentEmployees = absentEmployees,
                LastUpdated = DateTime.Now.ToString("HH:mm:ss"),
                UpdateDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            // Notify specific company group
            await _hubContext.Clients.Group($"Company_{companyCode}")
                .SendAsync("AttendanceUpdated", attendanceData);

            // Notify admin group (all companies)
            await _hubContext.Clients.Group("AllCompanies")
                .SendAsync("AttendanceUpdated", attendanceData);
        }

        public async Task NotifyAttendanceProcessing(bool isProcessing)
        {
            await _hubContext.Clients.All
                .SendAsync("AttendanceProcessing", new { IsProcessing = isProcessing, Timestamp = DateTime.Now });
        }

        public async Task NotifyError(string message, int? companyCode = null)
        {
            var errorData = new
            {
                Message = message,
                Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                CompanyCode = companyCode
            };

            if (companyCode.HasValue)
            {
                await _hubContext.Clients.Group($"Company_{companyCode}")
                    .SendAsync("AttendanceError", errorData);
            }
            else
            {
                await _hubContext.Clients.All
                    .SendAsync("AttendanceError", errorData);
            }
        }
    }
}