using Dapper;
using HRManagementSystem.Models.CostsDashboard;
using Microsoft.Data.SqlClient;

namespace HRManagementSystem.Data
{

    public class CostsDashboardRepository : ICostsDashboardRepository
    {
        private readonly string _newAttendanceConnectionString;

        public CostsDashboardRepository(IConfiguration configuration)
        {
            _newAttendanceConnectionString = configuration.GetConnectionString("NewAttendanceConnection");
        }

        public async Task<CostsSummary> GetCostsSummaryAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var categoriesString = categories != null && categories.Any()
                ? string.Join(",", categories.Select(c => $"'{c}'"))
                : "''";

            var sql = $@"
                SELECT 
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN da.PerDayCTC ELSE 0 END), 0) as TotalCost,
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Present' AND ISNULL(dct.CostType, 'INDIRECT') = 'DIRECT' 
                        THEN da.PerDayCTC ELSE 0 END), 0) as DirectCost,
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Present' AND ISNULL(dct.CostType, 'INDIRECT') = 'INDIRECT' 
                        THEN da.PerDayCTC ELSE 0 END), 0) as IndirectCost,
                    COUNT(DISTINCT CASE WHEN da.AttendanceStatus = 'Present' THEN da.EmployeeCode END) as TotalEmployees,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentDays,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentDays,
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Absent' THEN da.PerDayCTC ELSE 0 END), 0) as AbsentCost,
                    CASE WHEN COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) > 0 
                        THEN CAST(SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN da.PerDayCTC ELSE 0 END) / 
                                 COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as DECIMAL(18,2))
                        ELSE 0 
                    END as AverageDailyCost
                FROM DailyAttendance da
                LEFT JOIN DepartmentCostType dct ON da.Department = dct.DepartmentName AND dct.IsActive = 1
                WHERE da.AttendanceDate BETWEEN @StartDate AND @EndDate
                AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                AND ({(categories?.Any() == true ? $"da.Category IN ({categoriesString})" : "1=1")})";

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<CostsSummary>(sql, new
                {
                    CompanyCode = companyCode,
                    StartDate = startDate,
                    EndDate = endDate
                });

                return result ?? new CostsSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCostsSummaryAsync: {ex.Message}");
                return new CostsSummary();
            }
        }

        public async Task<AttendanceSummary> GetAttendanceSummaryAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var categoriesString = categories != null && categories.Any()
                ? string.Join(",", categories.Select(c => $"'{c}'"))
                : "''";

            var sql = $@"
                SELECT 
                    COUNT(DISTINCT da.EmployeeCode) as TotalEmployees,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END) as LeaveCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'WeekOff' THEN 1 END) as WeekOffCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Holiday' THEN 1 END) as HolidayCount,
                    CASE WHEN COUNT(*) > 0 
                        THEN CAST(COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) * 100.0 / COUNT(*) as DECIMAL(10,2))
                        ELSE 0 
                    END as AttendancePercentage,
                    CASE WHEN COUNT(*) > 0 
                        THEN CAST(COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) * 100.0 / COUNT(*) as DECIMAL(10,2))
                        ELSE 0 
                    END as AbsenteeismRate
                FROM DailyAttendance da
                WHERE da.AttendanceDate BETWEEN @StartDate AND @EndDate
                AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                AND ({(categories?.Any() == true ? $"da.Category IN ({categoriesString})" : "1=1")})";

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<AttendanceSummary>(sql, new
                {
                    CompanyCode = companyCode,
                    StartDate = startDate,
                    EndDate = endDate
                });

                return result ?? new AttendanceSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAttendanceSummaryAsync: {ex.Message}");
                return new AttendanceSummary();
            }
        }

        public async Task<List<DepartmentCostData>> GetDepartmentCostsAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var categoriesString = categories != null && categories.Any()
                ? string.Join(",", categories.Select(c => $"'{c}'"))
                : "''";

            var sql = $@"
                WITH DeptCosts AS (
                    SELECT 
                        ISNULL(da.Department, 'Unknown') as Department,
                        ISNULL(dct.CostType, 'INDIRECT') as CostType,
                        SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN da.PerDayCTC ELSE 0 END) as TotalCost,
                        SUM(CASE WHEN da.AttendanceStatus = 'Absent' THEN da.PerDayCTC ELSE 0 END) as AbsentCost,
                        COUNT(DISTINCT da.EmployeeCode) as EmployeeCount,
                        COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentDays,
                        COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentDays
                    FROM DailyAttendance da
                    LEFT JOIN DepartmentCostType dct ON da.Department = dct.DepartmentName AND dct.IsActive = 1
                    WHERE da.AttendanceDate BETWEEN @StartDate AND @EndDate
                    AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                    AND ({(categories?.Any() == true ? $"da.Category IN ({categoriesString})" : "1=1")})
                    GROUP BY da.Department, dct.CostType
                ),
                TotalCost AS (
                    SELECT ISNULL(SUM(TotalCost), 0) as GrandTotal
                    FROM DeptCosts
                )
                SELECT 
                    dc.Department,
                    dc.CostType,
                    CAST(dc.TotalCost as DECIMAL(18,2)) as TotalCost,
                    CAST(dc.AbsentCost as DECIMAL(18,2)) as AbsentCost,
                    dc.EmployeeCount,
                    dc.PresentDays,
                    dc.AbsentDays,
                    CASE WHEN tc.GrandTotal > 0 
                        THEN CAST((dc.TotalCost * 100.0 / tc.GrandTotal) as DECIMAL(10,2))
                        ELSE 0 
                    END as Percentage
                FROM DeptCosts dc
                CROSS JOIN TotalCost tc
                ORDER BY dc.TotalCost DESC";

            try
            {
                var result = await connection.QueryAsync<DepartmentCostData>(sql, new
                {
                    CompanyCode = companyCode,
                    StartDate = startDate,
                    EndDate = endDate
                });

                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartmentCostsAsync: {ex.Message}");
                return new List<DepartmentCostData>();
            }
        }

        public async Task<List<EmployeeCostData>> GetEmployeeCostsAsync(int companyCode, List<string> categories, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var categoriesString = categories != null && categories.Any()
                ? string.Join(",", categories.Select(c => $"'{c}'"))
                : "''";

            var sql = $@"
                SELECT 
                    ISNULL(c.CompanyName, 'Unknown') as CompanyName,
                    da.EmployeeCode,
                    ISNULL(da.PunchNo, '') as PunchNo,
                    ISNULL(da.EmployeeName, '') as EmployeeName,
                    ISNULL(da.Department, 'Unknown') as Department,
                    ISNULL(dct.CostType, 'INDIRECT') as CostType,
                    ISNULL(da.Designation, 'Unknown') as Designation,
                    ISNULL(da.Category, 'Unknown') as Category,
                    CAST(MAX(da.PerDayCTC) as DECIMAL(18,2)) as DailyCTC,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentDays,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentDays,
                    CAST(SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN da.PerDayCTC ELSE 0 END) as DECIMAL(18,2)) as TotalCost,
                    CAST(SUM(CASE WHEN da.AttendanceStatus = 'Absent' THEN da.PerDayCTC ELSE 0 END) as DECIMAL(18,2)) as AbsentCost,
                    MAX(da.AttendanceStatus) as AttendanceStatus
                FROM DailyAttendance da
                LEFT JOIN Companies c ON da.CompanyCode = c.CompanyCode
                LEFT JOIN DepartmentCostType dct ON da.Department = dct.DepartmentName AND dct.IsActive = 1
                WHERE da.AttendanceDate BETWEEN @StartDate AND @EndDate
                AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                AND ({(categories?.Any() == true ? $"da.Category IN ({categoriesString})" : "1=1")})
                GROUP BY 
                    c.CompanyName,
                    da.EmployeeCode,
                    da.PunchNo,
                    da.EmployeeName,
                    da.Department,
                    dct.CostType,
                    da.Designation,
                    da.Category
                ORDER BY TotalCost DESC";

            try
            {
                var result = await connection.QueryAsync<EmployeeCostData>(sql, new
                {
                    CompanyCode = companyCode,
                    StartDate = startDate,
                    EndDate = endDate
                });

                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmployeeCostsAsync: {ex.Message}");
                return new List<EmployeeCostData>();
            }
        }

        public async Task<List<string>> GetDepartmentsByCompanyAsync(int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var sql = @"
                SELECT DISTINCT Department
                FROM DailyAttendance
                WHERE (@CompanyCode = 0 OR CompanyCode = @CompanyCode)
                AND Department IS NOT NULL
                AND Department != ''
                ORDER BY Department";

            try
            {
                var result = await connection.QueryAsync<string>(sql, new { CompanyCode = companyCode });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartmentsByCompanyAsync: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetCategoriesByCompanyAsync(int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var sql = @"
                SELECT DISTINCT Category
                FROM DailyAttendance
                WHERE (@CompanyCode = 0 OR CompanyCode = @CompanyCode)
                AND Category IS NOT NULL
                AND Category != ''
                ORDER BY Category";

            try
            {
                var result = await connection.QueryAsync<string>(sql, new { CompanyCode = companyCode });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCategoriesByCompanyAsync: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<DepartmentCostType>> GetAllDepartmentCostTypesAsync()
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var sql = @"
                SELECT 
                    Id,
                    DepartmentName,
                    CostType,
                    IsActive,
                    CreatedDate,
                    UpdatedDate
                FROM DepartmentCostType
                WHERE IsActive = 1
                ORDER BY CostType, DepartmentName";

            try
            {
                var result = await connection.QueryAsync<DepartmentCostType>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllDepartmentCostTypesAsync: {ex.Message}");
                return new List<DepartmentCostType>();
            }
        }

        public async Task<bool> UpdateDepartmentCostTypeAsync(string departmentName, string costType)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var sql = @"
                UPDATE DepartmentCostType
                SET CostType = @CostType,
                    UpdatedDate = GETDATE()
                WHERE DepartmentName = @DepartmentName
                AND IsActive = 1";

            try
            {
                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    DepartmentName = departmentName,
                    CostType = costType
                });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateDepartmentCostTypeAsync: {ex.Message}");
                return false;
            }
        }
    }
}