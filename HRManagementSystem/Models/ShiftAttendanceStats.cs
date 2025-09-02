namespace HRManagementSystem.Models
{
    public class ShiftAttendanceStats
    {
        public string ShiftCode { get; set; }
        public string ShiftName { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentEmployees { get; set; }
        public int AbsentEmployees { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
}
