using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagementSystem.Models
{
    public class AttendanceSummaryViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int SelectedCompanyCode { get; set; }
        public string SelectedDepartment { get; set; }
        public string SelectedCategory { get; set; }
        public string SelectedDesignation { get; set; }
        public string SelectedAttendanceStatus { get; set; }
        public string SelectedLongAbsentOption { get; set; }
        public SelectList Companies { get; set; }
        public SelectList Departments { get; set; }
        public SelectList Categories { get; set; }
        public SelectList Designations { get; set; }
        public SelectList AttendanceStatuses { get; set; }
        public SelectList LongAbsentOptions { get; set; }
    }

    public class AttendanceSummaryData
    {
        public string CompanyName { get; set; }
        public string EmployeeCode { get; set; }
        public string PunchNo { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string Category { get; set; }
        public string Section { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? FirstPunchTime { get; set; }
        public string AttendanceStatus { get; set; }
        public decimal PerDayCTC { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public bool LongAbsent { get; set; } = false;
        public bool Layoff { get; set; } = false;
    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string SearchValue { get; set; }
        public int SortColumn { get; set; }
        public string SortDirection { get; set; }
        public List<string> Columns { get; set; }
        // Custom filters
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int CompanyCode { get; set; }
        public string Department { get; set; }
        public string Category { get; set; }
        public string Designation { get; set; }
        public string AttendanceStatus { get; set; }
        public string LongAbsentOption { get; set; } = "All";
        public string LayoffOption { get; set; } = "All";
    }

    public class AttendanceExportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int CompanyCode { get; set; }
        public string Department { get; set; }
        public string Category { get; set; }
        public string Designation { get; set; }
        public string AttendanceStatus { get; set; }
        public string LongAbsentOption { get; set; } = "All";
        public string LayoffOption { get; set; } = "All";
    }

    public class DataTableResponse<T>
    {
        public List<T> Data { get; set; }
        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
    }


}