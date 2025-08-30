using HRManagementSystem.Models;

namespace HRManagementSystem.Data
{
    public interface ICompanyRepository
    {
        Task<List<Company>> GetCompaniesAsync();
        Task<List<Company>> GetCompaniesByUserRoleAsync(int roleId, int userCompanyCode);
        Task<bool> AddDepartmentAsync(string departmentName, int companyCode);

    }
}