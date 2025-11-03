using Dapper;
using HRManagementSystem.Models;
using Microsoft.Data.SqlClient;

namespace HRManagementSystem.Data
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly string _connectionString;

        public CompanyRepository(IConfiguration configuration)
        {
            // CHANGED: Use NewAttendanceConnection instead of DefaultConnection
            _connectionString = configuration.GetConnectionString("NewAttendanceConnection");
        }

        public async Task<List<Company>> GetCompaniesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT CompanyCode, CompanyName, cyShortName, IsActive, CreatedDate FROM Companies WHERE IsActive = 1";
            var result = await connection.QueryAsync<Company>(sql);
            return result.ToList();
        }

        //public async Task<List<Company>> GetCompaniesByUserRoleAsync(int roleId, int? userCompanyCode)
        //{
        //    using var connection = new SqlConnection(_connectionString);
        //    string sql;

        //    if (roleId == 1) // Admin - can see all companies
        //    {
        //        sql = "SELECT CompanyCode, CompanyName, cyShortName, IsActive, CreatedDate FROM Companies WHERE IsActive = 1";
        //        var result = await connection.QueryAsync<Company>(sql);
        //        return result.ToList();
        //    }
        //    else // HR, User, GM - can see only their company
        //    {
        //        sql = "SELECT CompanyCode, CompanyName, cyShortName, IsActive, CreatedDate FROM Companies WHERE CompanyCode = @CompanyCode AND IsActive = 1";
        //        var result = await connection.QueryAsync<Company>(sql, new { CompanyCode = userCompanyCode });
        //        return result.ToList();
        //    }
        //}
        public async Task<List<Company>> GetCompaniesByUserRoleAsync(int roleId, int userCompanyCode)
        {
            // Quick implementation - you can improve this later
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
        SELECT CompanyCode, CompanyName 
        FROM Companies 
        WHERE IsActive = 1 
        ORDER BY CompanyName";

            var companies = await connection.QueryAsync<Company>(sql);
            return companies.ToList();
        }
        #region 'Add Department view'
        public async Task<bool> AddDepartmentAsync(string departmentName, int companyCode)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // Check if department already exists
                var checkSql = @"
            SELECT COUNT(*) FROM payAttribute 
            WHERE AttributeName = @DepartmentName 
            AND AttributeID = '2' 
            AND CompanyCode = @CompanyCode";

                var exists = await connection.QuerySingleAsync<int>(checkSql, new { DepartmentName = departmentName, CompanyCode = companyCode });

                if (exists > 0)
                {
                    return false; // Department already exists
                }

                // Get next AttributeCode
                var maxCodeSql = @"
            SELECT ISNULL(MAX(CAST(AttributeCode AS INT)), 0) + 1 
            FROM payAttribute 
            WHERE AttributeID = '2'";

                var nextCode = await connection.QuerySingleAsync<int>(maxCodeSql);

                // Insert new department
                var insertSql = @"
            INSERT INTO payAttribute (AttributeID, AttributeCode, AttributeName, CompanyCode, IsActive, CreatedDate)
            VALUES ('2', @AttributeCode, @AttributeName, @CompanyCode, 1, @CreatedDate)";

                await connection.ExecuteAsync(insertSql, new
                {
                    AttributeCode = nextCode.ToString(),
                    AttributeName = departmentName,
                    CompanyCode = companyCode,
                    CreatedDate = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding department: {ex.Message}");
                return false;
            }
        }


        // --- Add this method ---
        public async Task<Company> GetCompanyByIdAsync(int companyCode)
        {
            if (companyCode == 0) return null; // Handle "ALL" case gracefully

            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT CompanyCode, CompanyName, cyShortName, IsActive, CreatedDate FROM Companies WHERE CompanyCode = @CompanyCode AND IsActive = 1";
            try
            {
                var company = await connection.QueryFirstOrDefaultAsync<Company>(sql, new { CompanyCode = companyCode });
                return company; // Returns null if not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting company by ID {companyCode}: {ex.Message}");
                return null; // Return null on error
            }
        }
        // --- End Added Method ---
        #endregion
    }
}