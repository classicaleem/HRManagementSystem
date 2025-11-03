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

        // Helper to safely format strings for IN clause
        private string FormatInClause(List<string> items)
        {
            if (items == null || !items.Any()) return "NULL"; // Return NULL to effectively match nothing if empty list provided
            // Basic sanitization
            return string.Join(",", items.Select(i => $"'{i?.Replace("'", "''")}'"));
        }

        // Creates the WHERE clause part for filtering
        private string CreateWhereClause(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate)
        {
            string categoryFilter = (categories != null && categories.Any()) ? $"da.Category IN ({FormatInClause(categories)})" : "1=1";
            // Filter by MainCostType if provided
            string mainCostTypeFilter = (mainCostTypes != null && mainCostTypes.Any()) ? $"dct.MainCostType IN ({FormatInClause(mainCostTypes)})" : "1=1";
            string companyFilter = (companyCode == 0) ? "1=1" : "da.CompanyCode = @CompanyCode";

            return $@"WHERE da.AttendanceDate BETWEEN @StartDate AND @EndDate
                      AND {companyFilter}
                      AND {categoryFilter}
                      AND {mainCostTypeFilter}";
        }


        public async Task<CostsSummary> GetCostsSummaryAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            string whereClause = CreateWhereClause(companyCode, categories, mainCostTypes, startDate, endDate);

            var sql = $@"
                SELECT
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN da.PerDayCTC ELSE 0 END), 0) as PresentCost,
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Present' AND ISNULL(dct.CostType, 'INDIRECT') = 'DIRECT' THEN da.PerDayCTC ELSE 0 END), 0) as DirectCost,
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Present' AND ISNULL(dct.CostType, 'INDIRECT') = 'INDIRECT' THEN da.PerDayCTC ELSE 0 END), 0) as IndirectCost,
                    COUNT(DISTINCT da.EmployeeCode) as TotalEmployees, -- Count distinct employees matching criteria
                    COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentDays,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentDays,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END) as LeaveDays,
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Absent' THEN da.PerDayCTC ELSE 0 END), 0) as AbsentCost,
                    ISNULL(SUM(CASE WHEN da.AttendanceStatus = 'Leave' THEN da.PerDayCTC ELSE 0 END), 0) as LeaveCost,
                    CASE WHEN COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) > 0
                        THEN CAST(SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN da.PerDayCTC ELSE 0 END) /
                                COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as DECIMAL(18,2))
                        ELSE 0
                    END as AverageDailyCost
                FROM DailyAttendance da
                LEFT JOIN DepartmentCostType dct ON da.Department = dct.DepartmentName -- Join to get MainCostType
                {whereClause}";

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<CostsSummary>(sql, new { CompanyCode = companyCode, StartDate = startDate, EndDate = endDate });
                return result ?? new CostsSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCostsSummaryAsync: {ex.Message}\nSQL: {sql}");
                return new CostsSummary();
            }
        }

        public async Task<AttendanceSummary> GetAttendanceSummaryAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            string whereClause = CreateWhereClause(companyCode, categories, mainCostTypes, startDate, endDate);

            // Calculate P/(P+A+L) for Attendance %
            var sql = $@"
                SELECT
                    COUNT(DISTINCT da.EmployeeCode) as TotalEmployees,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END) as LeaveCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'WeekOff' THEN 1 END) as WeekOffCount,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Holiday' THEN 1 END) as HolidayCount,
                    (COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) +
                     COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) +
                     COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END)) as TotalWorkDays,

                    CASE WHEN (COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END)) > 0
                        THEN CAST(COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) * 100.0 /
                                (COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END)) as DECIMAL(10,2))
                        ELSE 0
                    END as AttendancePercentage,

                    CASE WHEN (COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END)) > 0
                        THEN CAST(COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) * 100.0 /
                                (COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) + COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END)) as DECIMAL(10,2))
                        ELSE 0
                    END as AbsenteeismRate
                FROM DailyAttendance da
                LEFT JOIN DepartmentCostType dct ON da.Department = dct.DepartmentName
                {whereClause}";

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<AttendanceSummary>(sql, new { CompanyCode = companyCode, StartDate = startDate, EndDate = endDate });
                return result ?? new AttendanceSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAttendanceSummaryAsync: {ex.Message}\nSQL: {sql}");
                return new AttendanceSummary();
            }
        }

        public async Task<List<MainCostTypeSummary>> GetMainCostTypeSummaryAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            string whereClause = CreateWhereClause(companyCode, categories, mainCostTypes, startDate, endDate);

            var sql = $@"
                WITH CostData AS (
                    SELECT
                        ISNULL(dct.MainCostType, 'Unknown') as MainCostType,
                        ISNULL(dct.CostType, 'INDIRECT') as CostType, -- Direct/Indirect
                        da.PerDayCTC,
                        da.AttendanceStatus
                    FROM DailyAttendance da
                    LEFT JOIN DepartmentCostType dct ON da.Department = dct.DepartmentName
                    {whereClause}
                ),
                GroupedCosts AS (
                    SELECT
                        MainCostType,
                        ISNULL(SUM(CASE WHEN AttendanceStatus = 'Present' THEN PerDayCTC ELSE 0 END), 0) as PresentCost,
                        ISNULL(SUM(CASE WHEN AttendanceStatus = 'Present' AND CostType = 'DIRECT' THEN PerDayCTC ELSE 0 END), 0) as DirectCost,
                        ISNULL(SUM(CASE WHEN AttendanceStatus = 'Present' AND CostType = 'INDIRECT' THEN PerDayCTC ELSE 0 END), 0) as IndirectCost,
                        ISNULL(SUM(CASE WHEN AttendanceStatus = 'Absent' THEN PerDayCTC ELSE 0 END), 0) as AbsentCost,
                        ISNULL(SUM(CASE WHEN AttendanceStatus = 'Leave' THEN PerDayCTC ELSE 0 END), 0) as LeaveCost
                    FROM CostData
                    GROUP BY MainCostType
                ),
                TotalOverallPresentCost AS (
                    SELECT ISNULL(SUM(PresentCost), 1) as GrandTotalPresent -- Base % on total present cost, avoid div by zero
                    FROM GroupedCosts
                )
                SELECT
                    gc.MainCostType,
                    CAST(gc.PresentCost as DECIMAL(18,2)) as PresentCost,
                    CAST(gc.DirectCost as DECIMAL(18,2)) as DirectCost,
                    CAST(gc.IndirectCost as DECIMAL(18,2)) as IndirectCost,
                    CAST(gc.AbsentCost as DECIMAL(18,2)) as AbsentCost,
                    CAST(gc.LeaveCost as DECIMAL(18,2)) as LeaveCost,
                    CAST((gc.PresentCost * 100.0 / topc.GrandTotalPresent) as DECIMAL(10,2)) as PercentageOfTotalPresent
                FROM GroupedCosts gc
                CROSS JOIN TotalOverallPresentCost topc
                WHERE gc.PresentCost > 0 OR gc.AbsentCost > 0 OR gc.LeaveCost > 0 -- Include if any cost exists
                ORDER BY gc.PresentCost DESC";

            try
            {
                var result = await connection.QueryAsync<MainCostTypeSummary>(sql, new { CompanyCode = companyCode, StartDate = startDate, EndDate = endDate });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMainCostTypeSummaryAsync: {ex.Message}\nSQL: {sql}");
                return new List<MainCostTypeSummary>();
            }
        }


        public async Task<List<EmployeeCostData>> GetEmployeeCostsAsync(int companyCode, List<string> categories, List<string> mainCostTypes, DateTime startDate, DateTime endDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            string whereClause = CreateWhereClause(companyCode, categories, mainCostTypes, startDate, endDate);

            // Fetch MainCostType along with other details
            var sql = $@"
                SELECT
                    ISNULL(c.CompanyName, 'Unknown') as CompanyName,
                    da.EmployeeCode,
                    ISNULL(da.PunchNo, '') as PunchNo,
                    ISNULL(da.EmployeeName, '') as EmployeeName,
                    ISNULL(da.Department, 'Unknown') as Department,
                    ISNULL(dct.CostType, 'INDIRECT') as CostType,
                    ISNULL(dct.MainCostType, 'Unknown') as MainCostType, -- Added MainCostType
                    ISNULL(da.Designation, 'Unknown') as Designation,
                    ISNULL(da.Category, 'Unknown') as Category,
                    CAST(MAX(ISNULL(da.PerDayCTC, 0)) as DECIMAL(18,2)) as DailyCTC,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 END) as PresentDays,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 END) as AbsentDays,
                    COUNT(CASE WHEN da.AttendanceStatus = 'Leave' THEN 1 END) as LeaveDays,
                    CAST(SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN ISNULL(da.PerDayCTC, 0) ELSE 0 END) as DECIMAL(18,2)) as TotalCost, -- Present Cost
                    CAST(SUM(CASE WHEN da.AttendanceStatus = 'Absent' THEN ISNULL(da.PerDayCTC, 0) ELSE 0 END) as DECIMAL(18,2)) as AbsentCost,
                    CAST(SUM(CASE WHEN da.AttendanceStatus = 'Leave' THEN ISNULL(da.PerDayCTC, 0) ELSE 0 END) as DECIMAL(18,2)) as LeaveCost
                FROM DailyAttendance da
                LEFT JOIN Companies c ON da.CompanyCode = c.CompanyCode
                LEFT JOIN DepartmentCostType dct ON da.Department = dct.DepartmentName
                {whereClause}
                GROUP BY
                    c.CompanyName,
                    da.EmployeeCode, da.PunchNo, da.EmployeeName, da.Department,
                    dct.CostType, dct.MainCostType, -- Added MainCostType
                    da.Designation, da.Category
                ORDER BY EmployeeName";

            try
            {
                var result = await connection.QueryAsync<EmployeeCostData>(sql, new { CompanyCode = companyCode, StartDate = startDate, EndDate = endDate });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmployeeCostsAsync: {ex.Message}\nSQL: {sql}");
                return new List<EmployeeCostData>();
            }
        }

        // --- GetDepartmentsByCompanyAsync removed as it's no longer the primary filter ---
        public async Task<List<string>> GetDepartmentsByCompanyAsync(int companyCode)
        {
            // This might still be useful for other parts of the application or future drill-downs
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            var sql = @"SELECT DISTINCT Department FROM DailyAttendance WHERE (@CompanyCode = 0 OR CompanyCode = @CompanyCode) AND Department IS NOT NULL AND Department != '' ORDER BY Department";
            try { var result = await connection.QueryAsync<string>(sql, new { CompanyCode = companyCode }); return result.ToList(); }
            catch (Exception ex) { Console.WriteLine($"Error in GetDepartmentsByCompanyAsync: {ex.Message}"); return new List<string>(); }
        }


        public async Task<List<string>> GetCategoriesByCompanyAsync(int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            var sql = @"SELECT DISTINCT Category FROM DailyAttendance WHERE (@CompanyCode = 0 OR CompanyCode = @CompanyCode) AND Category IS NOT NULL AND Category != '' ORDER BY Category";
            try { var result = await connection.QueryAsync<string>(sql, new { CompanyCode = companyCode }); return result.ToList(); }
            catch (Exception ex) { Console.WriteLine($"Error in GetCategoriesByCompanyAsync: {ex.Message}"); return new List<string>(); }
        }

        public async Task<List<string>> GetMainCostTypesByCompanyAsync(int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            // Select distinct MainCostType values from DepartmentCostType, potentially joining with DailyAttendance if filtering by company is strict
            var sql = @"
                SELECT DISTINCT dct.MainCostType
                FROM DepartmentCostType dct
                INNER JOIN DailyAttendance da ON dct.DepartmentName = da.Department -- Join to filter by company if needed
                WHERE dct.MainCostType IS NOT NULL AND dct.MainCostType != ''
                  AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode) -- Filter based on company if specified
                ORDER BY dct.MainCostType";
            try
            {
                var result = await connection.QueryAsync<string>(sql, new { CompanyCode = companyCode });
                return result.ToList();
            }
            catch (Exception ex) { Console.WriteLine($"Error in GetMainCostTypesByCompanyAsync: {ex.Message}"); return new List<string>(); }
        }


        public async Task<List<DepartmentCostType>> GetAllDepartmentCostTypesAsync()
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            var sql = @"SELECT Id, DepartmentName, CostType, MainCostType, IsActive, CreatedDate, UpdatedDate FROM DepartmentCostType WHERE IsActive = 1 ORDER BY MainCostType, CostType, DepartmentName"; // Added MainCostType
            try { var result = await connection.QueryAsync<DepartmentCostType>(sql); return result.ToList(); }
            catch (Exception ex) { Console.WriteLine($"Error in GetAllDepartmentCostTypesAsync: {ex.Message}"); return new List<DepartmentCostType>(); }
        }

        // Updated signature, implementation needs modification if used
        public async Task<bool> UpdateDepartmentCostTypeAsync(string departmentName, string costType, string mainCostType)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);
            var sql = @"UPDATE DepartmentCostType SET CostType = @CostType, MainCostType = @MainCostType, UpdatedDate = GETDATE() WHERE DepartmentName = @DepartmentName AND IsActive = 1";
            try
            {
                var rowsAffected = await connection.ExecuteAsync(sql, new { DepartmentName = departmentName, CostType = costType, MainCostType = mainCostType });
                return rowsAffected > 0;
            }
            catch (Exception ex) { Console.WriteLine($"Error in UpdateDepartmentCostTypeAsync: {ex.Message}"); return false; }
        }
    }
}