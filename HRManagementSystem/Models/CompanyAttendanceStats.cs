namespace HRManagementSystem.Models
{
    public class CompanyAttendanceStats
    {
        public int CompanyCode { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentEmployees { get; set; }
        public int AbsentEmployees { get; set; }
    }
}