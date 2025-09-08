namespace HRManagementSystem.Models
{
    public class Employee
    {
        public int CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string PunchNo { get; set; }
        public string Dept { get; set; }
        public string Category { get; set; }
        public string Desig { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string EmployeeStatus { get; set; }
        public string MainSection { get; set; }
        public string PerDayCTC { get; set; }
        public bool LongAbsent { get; set; } = false;
        public bool Layoff { get; set; } = false;
        public string Shift { get; set; }

    }
}