using Microsoft.AspNetCore.Mvc.Rendering;
namespace HRManagementSystem.Models.Report
{
    public class StatisticsReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int SelectedCompanyCode { get; set; }
        public SelectList Companies { get; set; }

        public StatisticsReportViewModel()
        {
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            FromDate = today.AddDays(-diff);
            ToDate = FromDate.AddDays(6);
        }
    }

    public class StatisticsReportData
    {
        public DateTime AttendanceDate { get; set; }
        public string FormattedDate => AttendanceDate.ToString("dd-MMM-yy");
        public string SortableDate => AttendanceDate.ToString("yyyy-MM-dd");
        public int CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public int TotalEmployee { get; set; }
        public decimal TotalPercentage { get; set; } = 100;
        public int Present { get; set; }
        public decimal PresentPercentage { get; set; }
        public int Absent { get; set; }
        public decimal AbsentPercentage { get; set; }
        public int Layoff { get; set; }
        public decimal LayoffPercentage { get; set; }
    }

    public class StatisticsReportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int CompanyCode { get; set; }
        public string Category { get; set; }
        public string Department { get; set; }
    }
}