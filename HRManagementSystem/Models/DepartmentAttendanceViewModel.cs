// Models/DepartmentAttendanceViewModel.cs
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagementSystem.Models
{
    public class DepartmentAttendanceViewModel
    {
        public DateTime ReportDate { get; set; }
        public int SelectedCompanyCode { get; set; }
        public string SelectedDepartment { get; set; } = "ALL"; // Add department filter
        public List<Company> Companies { get; set; } = new(); // Use existing Company model
        public SelectList CompanySelectList { get; set; }
        public SelectList DepartmentSelectList { get; set; } // Add department dropdown
        public List<string> AvailableDepartments { get; set; } = new(); // Available departments
        public List<DepartmentAttendanceData> Departments { get; set; } = new();
        public DepartmentTotals GrandTotals { get; set; } = new();
    }

    public class DepartmentAttendanceData
    {
        public string DepartmentName { get; set; }
        public List<MainSectionData> MainSections { get; set; } = new();
        public DepartmentTotals DepartmentTotals { get; set; } = new();
    }

    public class MainSectionData
    {
        public string MainSectionName { get; set; }
        public List<NOWAttendanceData> NOWData { get; set; } = new();
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int Total { get; set; }
        public int TotalLayoff { get; set; } = 0;
    }

    public class NOWAttendanceData
    {
        public string NOWName { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Total { get; set; }
        public int Layoff { get; set; } = 0;
    }

    public class DepartmentTotals
    {
        public int AttacherPresent { get; set; }
        public int AttacherAbsent { get; set; }
        public int AttacherTotal { get; set; }
        public int AttacherLayoff { get; set; } = 0;

        public int FolderPresent { get; set; }
        public int FolderAbsent { get; set; }
        public int FolderLayoff { get; set; } = 0;
        public int FolderTotal { get; set; }

        public int OthersPresent { get; set; }
        public int OthersAbsent { get; set; }
        public int OthersLayoff { get; set; } = 0;
        public int OthersTotal { get; set; }

        public int SkiverPresent { get; set; }
        public int SkiverAbsent { get; set; }
        public int SkiverLayoff { get; set; } = 0;
        public int SkiverTotal { get; set; }

        public int StitcherPresent { get; set; }
        public int StitcherAbsent { get; set; }
        public int StitcherLayoff { get; set; } = 0;
        public int StitcherTotal { get; set; }

        public int TotalPresent { get; set; }
        public int TotalLayoff { get; set; } = 0;
        public int TotalAbsent { get; set; }
        public int GrandTotal { get; set; }
    }
    public class DepartmentSummaryResult
    {
        public string Department { get; set; }
        public string MainSection { get; set; }
        public string Designation { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int TotalCount { get; set; }
    }
}