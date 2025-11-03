using HRManagementSystem.Hrlper;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagementSystem.Models.CostsDashboard
{
    // NEW Class for the Main Chart Data
    public class MainCostTypeSummary
    {
        public string MainCostType { get; set; }
        public decimal PresentCost { get; set; }
        public decimal DirectCost { get; set; } // Direct portion of PresentCost
        public decimal IndirectCost { get; set; } // Indirect portion of PresentCost
        public decimal AbsentCost { get; set; }
        public decimal LeaveCost { get; set; }
        public decimal PercentageOfTotalPresent { get; set; } // % this MCT contributes to total present cost

        // Formatted properties
        public string FormattedPresentCost => FormattingHelpers.FormatCurrency(PresentCost);
        public string FormattedAbsentCost => FormattingHelpers.FormatCurrency(AbsentCost);
        public string FormattedLeaveCost => FormattingHelpers.FormatCurrency(LeaveCost);

        // Define colors here or in JS based on MainCostType
        public string ChartColor => MainCostType switch
        {
            "UPPER" => "#3498db", // Blue
            "FULL SHOE" => "#2ecc71", // Green
            "PED" => "#f1c40f", // Yellow
            _ => "#95a5a6" // Grey for Unknown/Other
        };
    }


    public class CostsDashboardViewModel
    {
        public int SelectedCompanyCode { get; set; }
        public List<string> SelectedCategories { get; set; }
        public List<string> SelectedMainCostTypes { get; set; } // Changed from Departments
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Dropdown lists
        public SelectList Companies { get; set; }
        public MultiSelectList Categories { get; set; }
        public MultiSelectList MainCostTypes { get; set; } // Changed from Departments

        // Dashboard data
        public CostsSummary Summary { get; set; }
        public AttendanceSummary AttendanceSummary { get; set; }
        public List<MainCostTypeSummary> MainCostTypeSummaries { get; set; } // Changed from DepartmentCosts
        public List<EmployeeCostData> EmployeeCosts { get; set; }

        public CostsDashboardViewModel()
        {
            SelectedCategories = new List<string>();
            SelectedMainCostTypes = new List<string>(); // Default to ALL (empty list)
            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now.Date;
            Summary = new CostsSummary();
            AttendanceSummary = new AttendanceSummary();
            MainCostTypeSummaries = new List<MainCostTypeSummary>();
            EmployeeCosts = new List<EmployeeCostData>();
        }
    }

    public class CostsSummary
    {
        // Properties remain the same (PresentCost, DirectCost, IndirectCost, AbsentCost, LeaveCost, etc.)
        public decimal PresentCost { get; set; }
        public decimal DirectCost { get; set; }
        public decimal IndirectCost { get; set; }
        public decimal AbsentCost { get; set; }
        public decimal LeaveCost { get; set; }
        public decimal AverageDailyCost { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }

        public decimal TotalCalculatedCost => PresentCost + AbsentCost + LeaveCost;
        public int TotalDays => PresentDays + AbsentDays + LeaveDays;

        public decimal PresentCostPercentage => (TotalCalculatedCost == 0) ? 0 : Math.Round((PresentCost / TotalCalculatedCost) * 100, 2);
        public decimal AbsentCostPercentage => (TotalCalculatedCost == 0) ? 0 : Math.Round((AbsentCost / TotalCalculatedCost) * 100, 2);
        public decimal LeaveCostPercentage => (TotalCalculatedCost == 0) ? 0 : Math.Round((LeaveCost / TotalCalculatedCost) * 100, 2);
        public decimal PresentDaysPercentage => (TotalDays == 0) ? 0 : Math.Round(((decimal)PresentDays / TotalDays) * 100, 2);
        public decimal AbsentDaysPercentage => (TotalDays == 0) ? 0 : Math.Round(((decimal)AbsentDays / TotalDays) * 100, 2);
        public decimal LeaveDaysPercentage => (TotalDays == 0) ? 0 : Math.Round(((decimal)LeaveDays / TotalDays) * 100, 2);
        public decimal DirectCostPercentage => (PresentCost == 0) ? 0 : Math.Round((DirectCost / PresentCost) * 100, 2);
        public decimal IndirectCostPercentage => (PresentCost == 0) ? 0 : Math.Round((IndirectCost / PresentCost) * 100, 2);

        public string FormattedTotalCost => FormattingHelpers.FormatCurrency(PresentCost);
        public string FormattedPresentCost => FormattingHelpers.FormatCurrency(PresentCost);
        public string FormattedAbsentCost => FormattingHelpers.FormatCurrency(AbsentCost);
        public string FormattedLeaveCost => FormattingHelpers.FormatCurrency(LeaveCost);
        public string FormattedAverageCost => FormattingHelpers.FormatCurrency(AverageDailyCost);
        public string FormattedTotalCalculatedCost => FormattingHelpers.FormatCurrency(TotalCalculatedCost);
    }

    public class AttendanceSummary
    {
        // Properties remain the same
        public int TotalEmployees { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public int WeekOffCount { get; set; }
        public int HolidayCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public decimal AbsenteeismRate { get; set; }
        public int TotalWorkDays { get; set; }
    }

    // DepartmentCostData class removed

    public class EmployeeCostData
    {
        public string CompanyName { get; set; }
        public string EmployeeCode { get; set; }
        public string PunchNo { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string CostType { get; set; } // DIRECT or INDIRECT
        public string MainCostType { get; set; } // Added
        public string Designation { get; set; }
        public string Category { get; set; }
        public decimal DailyCTC { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public decimal TotalCost { get; set; } // Present
        public decimal AbsentCost { get; set; }
        public decimal LeaveCost { get; set; }

        public bool IsDirect => CostType?.ToUpper() == "DIRECT";

        public string FormattedDailyCTC => FormattingHelpers.FormatCurrency(DailyCTC);
        public string FormattedTotalCost => FormattingHelpers.FormatCurrency(TotalCost);
        public string FormattedAbsentCost => FormattingHelpers.FormatCurrency(AbsentCost);
        public string FormattedLeaveCost => FormattingHelpers.FormatCurrency(LeaveCost);

        public decimal AttendancePercentage { get { /* calculation remains same */ var totalDays = PresentDays + AbsentDays + LeaveDays; return totalDays > 0 ? Math.Round((PresentDays * 100.0m) / totalDays, 2) : 0; } }
    }

    // Updated DepartmentCostType to include MainCostType
    public class DepartmentCostType
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; }
        public string CostType { get; set; } // DIRECT or INDIRECT
        public string MainCostType { get; set; } // Added e.g., "UPPER", "FULL SHOE", "PED"
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    // Make sure Company Model exists (add properties if needed)
    //public class Company
    //{
    //    public int CompanyCode { get; set; }
    //    public string CompanyName { get; set; }
    //    public string cyShortName { get; set; }
    //    public bool IsActive { get; set; }
    //    public DateTime CreatedDate { get; set; }
    //}
}