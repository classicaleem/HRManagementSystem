namespace HRManagementSystem.Services
{
    public interface IAttendanceNotificationService
    {
        Task NotifyAttendanceUpdated(int companyCode, int totalEmployees, int presentEmployees, int absentEmployees);
        Task NotifyAttendanceProcessing(bool isProcessing);
        Task NotifyError(string message, int? companyCode = null);
    }
}