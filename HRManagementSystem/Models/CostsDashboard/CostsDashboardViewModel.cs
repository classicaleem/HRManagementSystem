//using Microsoft.AspNetCore.Mvc.Rendering;

//namespace HRManagementSystem.Models.CostsDashboard
//{
//    /// <summary>
//    /// Main view model for Costs Dashboard page
//    /// </summary>
//    public class CostsDashboardViewModel
//    {
//        public int SelectedCompanyCode { get; set; }
//        public string SelectedDepartment { get; set; }
//        public DateTime StartDate { get; set; }
//        public DateTime EndDate { get; set; }

//        // Dropdown lists
//        public SelectList Companies { get; set; }
//        public SelectList Departments { get; set; }

//        // Dashboard data
//        public CostsSummary Summary { get; set; }
//        public List<DepartmentCostData> DepartmentCosts { get; set; }
//        public List<EmployeeCostData> EmployeeCosts { get; set; }

//        /// <summary>
//        /// Constructor with default initialization
//        /// </summary>
//        public CostsDashboardViewModel()
//        {
//            SelectedDepartment = "ALL";
//            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
//            EndDate = DateTime.Now.Date;
//            Summary = new CostsSummary();
//            DepartmentCosts = new List<DepartmentCostData>();
//            EmployeeCosts = new List<EmployeeCostData>();
//        }

//        /// <summary>
//        /// Gets total number of unique employees
//        /// </summary>
//        public int TotalEmployees => EmployeeCosts?.Select(e => e.EmployeeCode).Distinct().Count() ?? 0;

//        /// <summary>
//        /// Gets total of all present days
//        /// </summary>
//        public int TotalPresentDays => EmployeeCosts?.Sum(e => e.PresentDays) ?? 0;

//        /// <summary>
//        /// Gets count of DIRECT cost employees
//        /// </summary>
//        public int DirectEmployeeCount => EmployeeCosts?.Count(e => e.CostType == "DIRECT") ?? 0;

//        /// <summary>
//        /// Gets count of INDIRECT cost employees
//        /// </summary>
//        public int IndirectEmployeeCount => EmployeeCosts?.Count(e => e.CostType == "INDIRECT") ?? 0;
//    }

//    /// <summary>
//    /// Summary of all costs with direct/indirect breakdown
//    /// </summary>
//    public class CostsSummary
//    {
//        public decimal TotalCost { get; set; }
//        public decimal DirectCost { get; set; }
//        public decimal IndirectCost { get; set; }
//        public int TotalEmployees { get; set; }
//        public int PresentDays { get; set; }
//        public decimal AverageDailyCost { get; set; }

//        /// <summary>
//        /// Percentage of Direct Cost
//        /// </summary>
//        public decimal DirectCostPercentage
//        {
//            get
//            {
//                if (TotalCost == 0) return 0;
//                return Math.Round((DirectCost / TotalCost) * 100, 2);
//            }
//        }

//        /// <summary>
//        /// Percentage of Indirect Cost
//        /// </summary>
//        public decimal IndirectCostPercentage
//        {
//            get
//            {
//                if (TotalCost == 0) return 0;
//                return Math.Round((IndirectCost / TotalCost) * 100, 2);
//            }
//        }

//        /// <summary>
//        /// Formatted total cost
//        /// </summary>
//        public string FormattedTotalCost => $"₹{TotalCost:N2}";

//        /// <summary>
//        /// Formatted direct cost
//        /// </summary>
//        public string FormattedDirectCost => $"₹{DirectCost:N2}";

//        /// <summary>
//        /// Formatted indirect cost
//        /// </summary>
//        public string FormattedIndirectCost => $"₹{IndirectCost:N2}";

//        /// <summary>
//        /// Formatted average daily cost
//        /// </summary>
//        public string FormattedAverageCost => $"₹{AverageDailyCost:N2}";
//    }

//    /// <summary>
//    /// Department-wise cost breakdown with cost type classification
//    /// </summary>
//    public class DepartmentCostData
//    {
//        public string Department { get; set; }
//        public string CostType { get; set; } // DIRECT or INDIRECT
//        public decimal TotalCost { get; set; }
//        public int EmployeeCount { get; set; }
//        public decimal Percentage { get; set; }

//        /// <summary>
//        /// Returns true if this is a direct cost department
//        /// </summary>
//        public bool IsDirect => CostType?.ToUpper() == "DIRECT";

//        /// <summary>
//        /// Returns CSS class based on cost type
//        /// </summary>
//        public string CostTypeClass => IsDirect ? "badge-success" : "badge-warning";

//        /// <summary>
//        /// Returns background color for charts
//        /// </summary>
//        public string ChartColor => IsDirect ? "#28a745" : "#ffc107";

//        /// <summary>
//        /// Formatted total cost
//        /// </summary>
//        public string FormattedTotalCost => $"₹{TotalCost:N2}";

//        /// <summary>
//        /// Department display name with cost type
//        /// </summary>
//        public string DisplayName => $"{Department} ({CostType})";
//    }

//    /// <summary>
//    /// Individual employee cost data
//    /// </summary>
//    public class EmployeeCostData
//    {
//        public string CompanyName { get; set; }
//        public string EmployeeCode { get; set; }
//        public string PunchNo { get; set; }
//        public string EmployeeName { get; set; }
//        public string Department { get; set; }
//        public string CostType { get; set; } // DIRECT or INDIRECT
//        public string Designation { get; set; }
//        public string Category { get; set; }
//        public decimal DailyCTC { get; set; }
//        public int PresentDays { get; set; }
//        public decimal TotalCost { get; set; }

//        /// <summary>
//        /// Returns true if this employee is in a direct cost department
//        /// </summary>
//        public bool IsDirect => CostType?.ToUpper() == "DIRECT";

//        /// <summary>
//        /// Returns CSS class based on cost type
//        /// </summary>
//        public string CostTypeClass => IsDirect ? "badge-success" : "badge-warning";

//        /// <summary>
//        /// Returns formatted daily CTC
//        /// </summary>
//        public string FormattedDailyCTC => $"₹{DailyCTC:N2}";

//        /// <summary>
//        /// Returns formatted total cost
//        /// </summary>
//        public string FormattedTotalCost => $"₹{TotalCost:N2}";

//        /// <summary>
//        /// Returns cost type display with icon
//        /// </summary>
//        public string CostTypeDisplay
//        {
//            get
//            {
//                if (IsDirect)
//                    return "<span class='badge badge-success'><i class='fas fa-industry'></i> DIRECT</span>";
//                else
//                    return "<span class='badge badge-warning'><i class='fas fa-users-cog'></i> INDIRECT</span>";
//            }
//        }

//        /// <summary>
//        /// Full employee display name
//        /// </summary>
//        public string FullDisplayName => $"{EmployeeName} ({EmployeeCode})";
//    }

//    /// <summary>
//    /// Cost type summary for charts
//    /// </summary>
//    public class CostTypeData
//    {
//        public string Type { get; set; }
//        public decimal Amount { get; set; }
//        public decimal Percentage { get; set; }
//        public string Color { get; set; }
//        public int EmployeeCount { get; set; }

//        /// <summary>
//        /// Formatted amount
//        /// </summary>
//        public string FormattedAmount => $"₹{Amount:N2}";

//        /// <summary>
//        /// Display label with percentage
//        /// </summary>
//        public string DisplayLabel => $"{Type}: {FormattedAmount} ({Percentage}%)";
//    }

//    /// <summary>
//    /// Department cost type configuration
//    /// </summary>
//    public class DepartmentCostType
//    {
//        public int Id { get; set; }
//        public string DepartmentName { get; set; }
//        public string CostType { get; set; } // DIRECT or INDIRECT
//        public bool IsActive { get; set; }
//        public DateTime CreatedDate { get; set; }
//        public DateTime? UpdatedDate { get; set; }

//        /// <summary>
//        /// Returns true if this is a direct cost department
//        /// </summary>
//        public bool IsDirect => CostType?.ToUpper() == "DIRECT";

//        /// <summary>
//        /// Returns CSS class for badge
//        /// </summary>
//        public string BadgeClass => IsDirect ? "badge-success" : "badge-warning";

//        /// <summary>
//        /// Returns icon class
//        /// </summary>
//        public string IconClass => IsDirect ? "fa-industry" : "fa-users-cog";

//        /// <summary>
//        /// Returns formatted created date
//        /// </summary>
//        public string FormattedCreatedDate => CreatedDate.ToString("dd-MMM-yyyy");

//        /// <summary>
//        /// Returns formatted updated date
//        /// </summary>
//        public string FormattedUpdatedDate => UpdatedDate?.ToString("dd-MMM-yyyy HH:mm") ?? "Never";

//        /// <summary>
//        /// Returns status badge HTML
//        /// </summary>
//        public string StatusBadge
//        {
//            get
//            {
//                if (IsActive)
//                    return "<span class='badge badge-success'>Active</span>";
//                else
//                    return "<span class='badge badge-secondary'>Inactive</span>";
//            }
//        }

//        /// <summary>
//        /// Returns cost type badge HTML
//        /// </summary>
//        public string CostTypeBadge
//        {
//            get
//            {
//                if (IsDirect)
//                    return "<span class='badge badge-success'><i class='fas fa-industry'></i> DIRECT</span>";
//                else
//                    return "<span class='badge badge-warning'><i class='fas fa-users-cog'></i> INDIRECT</span>";
//            }
//        }
//    }

//    /// <summary>
//    /// Filter options for dashboard
//    /// </summary>
//    public class CostsDashboardFilter
//    {
//        public int CompanyCode { get; set; }
//        public string Department { get; set; }
//        public DateTime StartDate { get; set; }
//        public DateTime EndDate { get; set; }
//        public string CostType { get; set; } // ALL, DIRECT, INDIRECT

//        /// <summary>
//        /// Constructor with default values
//        /// </summary>
//        public CostsDashboardFilter()
//        {
//            CompanyCode = 0;
//            Department = "ALL";
//            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
//            EndDate = DateTime.Now.Date;
//            CostType = "ALL";
//        }

//        /// <summary>
//        /// Validates the filter
//        /// </summary>
//        public bool IsValid()
//        {
//            return StartDate <= EndDate;
//        }

//        /// <summary>
//        /// Returns date range display
//        /// </summary>
//        public string DateRangeDisplay => $"{StartDate:dd-MMM-yyyy} to {EndDate:dd-MMM-yyyy}";
//    }

//    /// <summary>
//    /// Export options for Excel
//    /// </summary>
//    public class CostsExportOptions
//    {
//        public bool IncludeSummary { get; set; } = true;
//        public bool IncludeCharts { get; set; } = false;
//        public bool GroupByDepartment { get; set; } = false;
//        public bool GroupByCostType { get; set; } = true;
//        public string FileName { get; set; }

//        /// <summary>
//        /// Constructor
//        /// </summary>
//        public CostsExportOptions()
//        {
//            FileName = $"EmployeeCosts_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagementSystem.Models.CostsDashboard
{
    /// <summary>
    /// Main view model for Costs Dashboard page
    /// </summary>
    public class CostsDashboardViewModel
    {
        public int SelectedCompanyCode { get; set; }
        public List<string> SelectedCategories { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Dropdown lists
        public SelectList Companies { get; set; }
        public SelectList Departments { get; set; }
        public MultiSelectList Categories { get; set; }

        // Dashboard data
        public CostsSummary Summary { get; set; }
        public AttendanceSummary AttendanceSummary { get; set; }
        public List<DepartmentCostData> DepartmentCosts { get; set; }
        public List<EmployeeCostData> EmployeeCosts { get; set; }

        /// <summary>
        /// Constructor with default initialization
        /// </summary>
        public CostsDashboardViewModel()
        {
            SelectedCategories = new List<string> { "Worker", "Growmore", "Dailywages", "MALE-WORKER", "FTC", "DW-450" };
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now.Date;
            Summary = new CostsSummary();
            AttendanceSummary = new AttendanceSummary();
            DepartmentCosts = new List<DepartmentCostData>();
            EmployeeCosts = new List<EmployeeCostData>();
        }

        /// <summary>
        /// Gets total number of unique employees
        /// </summary>
        public int TotalEmployees => EmployeeCosts?.Select(e => e.EmployeeCode).Distinct().Count() ?? 0;

        /// <summary>
        /// Gets total of all present days
        /// </summary>
        public int TotalPresentDays => EmployeeCosts?.Sum(e => e.PresentDays) ?? 0;

        /// <summary>
        /// Gets count of DIRECT cost employees
        /// </summary>
        public int DirectEmployeeCount => EmployeeCosts?.Count(e => e.CostType == "DIRECT") ?? 0;

        /// <summary>
        /// Gets count of INDIRECT cost employees
        /// </summary>
        public int IndirectEmployeeCount => EmployeeCosts?.Count(e => e.CostType == "INDIRECT") ?? 0;
    }

    /// <summary>
    /// Summary of all costs with direct/indirect breakdown
    /// </summary>
    public class CostsSummary
    {
        public decimal TotalCost { get; set; }
        public decimal DirectCost { get; set; }
        public decimal IndirectCost { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentDays { get; set; }
        public decimal AverageDailyCost { get; set; }
        public decimal AbsentCost { get; set; }
        public int AbsentDays { get; set; }

        /// <summary>
        /// Percentage of Direct Cost
        /// </summary>
        public decimal DirectCostPercentage
        {
            get
            {
                if (TotalCost == 0) return 0;
                return Math.Round((DirectCost / TotalCost) * 100, 2);
            }
        }

        /// <summary>
        /// Percentage of Indirect Cost
        /// </summary>
        public decimal IndirectCostPercentage
        {
            get
            {
                if (TotalCost == 0) return 0;
                return Math.Round((IndirectCost / TotalCost) * 100, 2);
            }
        }

        /// <summary>
        /// Formatted total cost
        /// </summary>
        public string FormattedTotalCost => $"₹{TotalCost:N2}";

        /// <summary>
        /// Formatted direct cost
        /// </summary>
        public string FormattedDirectCost => $"₹{DirectCost:N2}";

        /// <summary>
        /// Formatted indirect cost
        /// </summary>
        public string FormattedIndirectCost => $"₹{IndirectCost:N2}";

        /// <summary>
        /// Formatted absent cost
        /// </summary>
        public string FormattedAbsentCost => $"₹{AbsentCost:N2}";

        /// <summary>
        /// Formatted average daily cost
        /// </summary>
        public string FormattedAverageCost => $"₹{AverageDailyCost:N2}";
    }

    /// <summary>
    /// Attendance Summary
    /// </summary>
    public class AttendanceSummary
    {
        public int TotalEmployees { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public int WeekOffCount { get; set; }
        public int HolidayCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public decimal AbsenteeismRate { get; set; }
    }

    /// <summary>
    /// Department-wise cost breakdown with cost type classification
    /// </summary>
    public class DepartmentCostData
    {
        public string Department { get; set; }
        public string CostType { get; set; } // DIRECT or INDIRECT
        public decimal TotalCost { get; set; }
        public int EmployeeCount { get; set; }
        public decimal Percentage { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal AbsentCost { get; set; }

        /// <summary>
        /// Returns true if this is a direct cost department
        /// </summary>
        public bool IsDirect => CostType?.ToUpper() == "DIRECT";

        /// <summary>
        /// Returns CSS class based on cost type
        /// </summary>
        public string CostTypeClass => IsDirect ? "badge-success" : "badge-warning";

        /// <summary>
        /// Returns background color for charts
        /// </summary>
        public string ChartColor => IsDirect ? "#28a745" : "#ffc107";

        /// <summary>
        /// Formatted total cost
        /// </summary>
        public string FormattedTotalCost => $"₹{TotalCost:N2}";

        /// <summary>
        /// Formatted absent cost
        /// </summary>
        public string FormattedAbsentCost => $"₹{AbsentCost:N2}";

        /// <summary>
        /// Department display name with cost type
        /// </summary>
        public string DisplayName => $"{Department} ({CostType})";
    }

    /// <summary>
    /// Individual employee cost data
    /// </summary>
    public class EmployeeCostData
    {
        public string CompanyName { get; set; }
        public string EmployeeCode { get; set; }
        public string PunchNo { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string CostType { get; set; } // DIRECT or INDIRECT
        public string Designation { get; set; }
        public string Category { get; set; }
        public decimal DailyCTC { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AbsentCost { get; set; }
        public string AttendanceStatus { get; set; }

        /// <summary>
        /// Returns true if this employee is in a direct cost department
        /// </summary>
        public bool IsDirect => CostType?.ToUpper() == "DIRECT";

        /// <summary>
        /// Returns CSS class based on cost type
        /// </summary>
        public string CostTypeClass => IsDirect ? "badge-success" : "badge-warning";

        /// <summary>
        /// Returns CSS class based on attendance status
        /// </summary>
        public string AttendanceStatusClass
        {
            get
            {
                return AttendanceStatus?.ToUpper() switch
                {
                    "PRESENT" => "badge-success",
                    "ABSENT" => "badge-danger",
                    "LEAVE" => "badge-info",
                    "WEEKOFF" => "badge-secondary",
                    "HOLIDAY" => "badge-primary",
                    _ => "badge-secondary"
                };
            }
        }

        /// <summary>
        /// Returns formatted daily CTC
        /// </summary>
        public string FormattedDailyCTC => $"₹{DailyCTC:N2}";

        /// <summary>
        /// Returns formatted total cost
        /// </summary>
        public string FormattedTotalCost => $"₹{TotalCost:N2}";

        /// <summary>
        /// Returns formatted absent cost
        /// </summary>
        public string FormattedAbsentCost => $"₹{AbsentCost:N2}";

        /// <summary>
        /// Returns cost type display with icon
        /// </summary>
        public string CostTypeDisplay
        {
            get
            {
                if (IsDirect)
                    return "<span class='badge badge-success'><i class='fas fa-industry'></i> DIRECT</span>";
                else
                    return "<span class='badge badge-warning'><i class='fas fa-users-cog'></i> INDIRECT</span>";
            }
        }

        /// <summary>
        /// Full employee display name
        /// </summary>
        public string FullDisplayName => $"{EmployeeName} ({EmployeeCode})";

        /// <summary>
        /// Attendance percentage
        /// </summary>
        public decimal AttendancePercentage
        {
            get
            {
                var totalDays = PresentDays + AbsentDays;
                return totalDays > 0 ? Math.Round((PresentDays * 100.0m) / totalDays, 2) : 0;
            }
        }
    }

    /// <summary>
    /// Department cost type configuration
    /// </summary>
    public class DepartmentCostType
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; }
        public string CostType { get; set; } // DIRECT or INDIRECT
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Returns true if this is a direct cost department
        /// </summary>
        public bool IsDirect => CostType?.ToUpper() == "DIRECT";

        /// <summary>
        /// Returns CSS class for badge
        /// </summary>
        public string BadgeClass => IsDirect ? "badge-success" : "badge-warning";

        /// <summary>
        /// Returns icon class
        /// </summary>
        public string IconClass => IsDirect ? "fa-industry" : "fa-users-cog";

        /// <summary>
        /// Returns formatted created date
        /// </summary>
        public string FormattedCreatedDate => CreatedDate.ToString("dd-MMM-yyyy");

        /// <summary>
        /// Returns formatted updated date
        /// </summary>
        public string FormattedUpdatedDate => UpdatedDate?.ToString("dd-MMM-yyyy HH:mm") ?? "Never";

        /// <summary>
        /// Returns status badge HTML
        /// </summary>
        public string StatusBadge
        {
            get
            {
                if (IsActive)
                    return "<span class='badge badge-success'>Active</span>";
                else
                    return "<span class='badge badge-secondary'>Inactive</span>";
            }
        }

        /// <summary>
        /// Returns cost type badge HTML
        /// </summary>
        public string CostTypeBadge
        {
            get
            {
                if (IsDirect)
                    return "<span class='badge badge-success'><i class='fas fa-industry'></i> DIRECT</span>";
                else
                    return "<span class='badge badge-warning'><i class='fas fa-users-cog'></i> INDIRECT</span>";
            }
        }
    }
}