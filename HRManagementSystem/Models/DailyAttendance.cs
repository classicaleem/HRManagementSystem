namespace HRManagementSystem.Models
{
    public class DailyAttendance
    {
        public int AttendanceId { get; set; }
        public int CompanyCode { get; set; }
        public string EmployeeCode { get; set; }
        public string PunchNo { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? FirstPunchTime { get; set; }
        public string AttendanceStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}