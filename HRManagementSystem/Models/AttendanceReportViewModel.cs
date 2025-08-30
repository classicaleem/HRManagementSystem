using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagementSystem.Models
{

    public class AttendanceReportViewModel
    {
        public List<AttendanceByDesignation> AttendanceByDesignations { get; set; }
        public DateTime ReportDate { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentEmployees { get; set; }
        public int AbsentEmployees { get; set; }
        public List<Company> Companies { get; set; }
        public int SelectedCompanyCode { get; set; }

        // ADD THIS: SelectList for better dropdown handling
        public SelectList CompanySelectList { get; set; }
    }
    public class AttendanceByDesignation
    {
        public string ParentDesignation { get; set; }
        public List<AttendanceBySubDesignation> SubDesignations { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentEmployees { get; set; }
        public int AbsentEmployees { get; set; }
        public int WorkerPresent { get; set; }
        public int StaffPresent { get; set; }
        public int OfficerPresent { get; set; }
        public int ManagerPresent { get; set; }
        public int ExecutivePresent { get; set; }
        public int OtherPresent { get; set; }
    }
    public class DailyAttendanceData
    {
        public string ParentDesignation { get; set; }
        public string SubDesignation { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Total { get; set; }
        public int WorkerPresent { get; set; }
        public int StaffPresent { get; set; }
        public int OfficerPresent { get; set; }
        public int ManagerPresent { get; set; }
        public int ExecutivePresent { get; set; }
        public int OtherPresent { get; set; }
    }
    public class AttendanceBySubDesignation
    {
        public string SubDesignation { get; set; }
        public int Attacher { get; set; }
        public int Folder { get; set; }
        public int Sticher { get; set; }
        public int Others { get; set; }
        public int Absent { get; set; }
        public int Present { get; set; }
        public int Total { get; set; }
        public int WorkerPresent { get; set; }
        public int StaffPresent { get; set; }
        public int OfficerPresent { get; set; }
        public int ManagerPresent { get; set; }
        public int ExecutivePresent { get; set; }
        public int OtherPresent { get; set; }
    }
}