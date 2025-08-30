using Dapper;
using HRManagementSystem.Models;
using Microsoft.Data.SqlClient;

namespace HRManagementSystem.Data
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<Employee>> GetEmployeesAsync(int companyCode)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT CompanyCode, cyShortName as CompanyName, EmployeeCode, EmployeeName, 
                       Punchno, Dept, Category, Desig, Gender, DateOfJoining, EmployeeStatus
                FROM vw_cEmployeeMaster 
                WHERE CompanyCode = @CompanyCode AND EmployeeStatus = 'WORKING'
                ORDER BY EmployeeName";

            var result = await connection.QueryAsync<Employee>(sql, new { CompanyCode = companyCode });
            return result.ToList();
        }

        public async Task<Employee> GetEmployeeByPunchNoAsync(string punchNo, int companyCode)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT CompanyCode, cyShortName as CompanyName, EmployeeCode, EmployeeName, 
                       Punchno, Dept, Category, Desig, Gender, DateOfJoining, EmployeeStatus
                FROM vw_cEmployeeMaster 
                WHERE Punchno = @PunchNo AND CompanyCode = @CompanyCode";

            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { PunchNo = punchNo, CompanyCode = companyCode });
        }
    }
}