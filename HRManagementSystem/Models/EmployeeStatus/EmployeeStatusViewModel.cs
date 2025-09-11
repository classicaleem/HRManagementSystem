using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagementSystem.Models.EmployeeStatus
{
    public class EmployeeStatusViewModel
    {
        public int SelectedCompanyCode { get; set; }
        public string SelectedDepartment { get; set; }
        public string SelectedCategory { get; set; }
        public string SelectedDesignation { get; set; }
        public SelectList Companies { get; set; }
        public SelectList Departments { get; set; }
        public SelectList Categories { get; set; }
        public SelectList Designations { get; set; }
    }

    public class EmployeeStatusData
    {
        public string CompanyName { get; set; }
        public string EmployeeCode { get; set; }
        public string PunchNo { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string Category { get; set; }
        public string Section { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string EmployeeStatus { get; set; }
        public bool LongAbsent { get; set; } = false;
        public bool Layoff { get; set; } = false;
        public string Shift { get; set; } = "G";
        //public DateTime? FirstPunchTime { get; set; }
        //public string AttendanceStatus { get; set; }

        public string FirstPunchTime { get; set; }
        public string AttendanceStatus { get; set; }

    }

    public class EmployeeStatusDataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string SearchValue { get; set; }
        public int SortColumn { get; set; }
        public string SortDirection { get; set; }
        public List<string> Columns { get; set; }
        // Custom filters
        public int CompanyCode { get; set; }
        public string Department { get; set; }
        public string Category { get; set; }
        public string Designation { get; set; }
        public string StatusFilter { get; set; } = "All"; // All, LongAbsent, Layoff, Active
    }

    public class EmployeeStatusBulkUpdateRequest
    {
        public List<string> EmployeeCodes { get; set; } = new List<string>();
        public bool? LongAbsent { get; set; }
        public bool? Layoff { get; set; }
        public string Shift { get; set; }
        public string Remarks { get; set; }
    }

    public class EmployeeStatusUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int UpdatedCount { get; set; }
        public List<string> FailedEmployees { get; set; } = new List<string>();
    }

    public class EmployeeStatusDataTableResponse<T>
    {
        public List<T> Data { get; set; }
        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
    }
}