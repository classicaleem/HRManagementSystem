using HRManagementSystem.Models;

namespace HRManagementSystem.Data
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetEmployeesAsync(int companyCode);
        Task<Employee> GetEmployeeByPunchNoAsync(string punchNo, int companyCode);
    }
}