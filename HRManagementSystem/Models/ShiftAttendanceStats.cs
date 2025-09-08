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
        public int LayoffEmployees { get; set; } = 0;
    }

    //layoff add new supporting classes
    public class DailyAttendanceDataWithLayoff
    {
        public string ParentDesignation { get; set; }
        public string SubDesignation { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Layoff { get; set; } = 0;
        public int Total { get; set; }
        public int WorkerPresent { get; set; }
        public int StaffPresent { get; set; }
        public int OfficerPresent { get; set; }
        public int ManagerPresent { get; set; }
        public int ExecutivePresent { get; set; }
        public int OtherPresent { get; set; }
    }

    public class ShiftAttendanceStatsWithLayoff
    {
        public string ShiftCode { get; set; }
        public string ShiftName { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentEmployees { get; set; }
        public int AbsentEmployees { get; set; }
        public int LayoffEmployees { get; set; } = 0;
        public int ActiveEmployees { get; set; }
        public decimal AttendancePercentage { get; set; }
    }

    public class DepartmentSummaryResultWithLayoff
    {
        public string Department { get; set; }
        public string MainSection { get; set; }
        public string Designation { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LayoffCount { get; set; } // NEW
        public int TotalCount { get; set; }
    }
}
