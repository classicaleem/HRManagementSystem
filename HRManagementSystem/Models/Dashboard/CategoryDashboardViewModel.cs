namespace HRManagementSystem.Models.Dashboard
{
    public class CategoryDashboardViewModel
    {
        public List<Company> Companies { get; set; } = new();
        public int SelectedCompanyCode { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public int UserCompanyCode { get; set; }
        public List<string> AvailableCategories { get; set; } = new();
        public List<string> SelectedCategories { get; set; } = new();
    }

    public class CategoryDashboardRequest
    {
        public int CompanyCode { get; set; }
        public List<string> Categories { get; set; } = new();
        public int Days { get; set; } = 7;
        public bool IncludeLayoff { get; set; } = false; // NEW
    }
}
