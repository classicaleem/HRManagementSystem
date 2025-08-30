namespace HRManagementSystem.Models
{
    public class Company
    {
        public int CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string cyShortName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}