using Dapper;
using HRManagementSystem.Models;
using Microsoft.Data.SqlClient;
using System.Text;

namespace HRManagementSystem.Data
{
    public class AttendanceSummaryRepository : IAttendanceSummaryRepository
    {
        private readonly string _newAttendanceConnectionString;

        public AttendanceSummaryRepository(IConfiguration configuration)
        {
            _newAttendanceConnectionString = configuration.GetConnectionString("NewAttendanceConnection");
        }

        public async Task<DataTableResponse<AttendanceSummaryData>> GetAttendanceSummaryAsync(DataTableRequest request)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            // Build base query
            var baseQuery = @"
                FROM vw_AttendanceSummaryReport
                WHERE AttendanceDate BETWEEN @FromDate AND @ToDate";

            var parameters = new DynamicParameters();
            parameters.Add("FromDate", request.FromDate);
            parameters.Add("ToDate", request.ToDate);

            // Apply filters
            var whereClause = new StringBuilder();

            if (request.CompanyCode > 0)
            {
                whereClause.Append(" AND CompanyCode = @CompanyCode");
                parameters.Add("CompanyCode", request.CompanyCode);
            }

            if (!string.IsNullOrEmpty(request.Department))
            {
                whereClause.Append(" AND Department = @Department");
                parameters.Add("Department", request.Department);
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                whereClause.Append(" AND Category = @Category");
                parameters.Add("Category", request.Category);
            }

            if (!string.IsNullOrEmpty(request.Designation))
            {
                whereClause.Append(" AND Designation = @Designation");
                parameters.Add("Designation", request.Designation);
            }

            if (!string.IsNullOrEmpty(request.AttendanceStatus) && request.AttendanceStatus != "All")
            {
                whereClause.Append(" AND AttendanceStatus = @AttendanceStatus");
                parameters.Add("AttendanceStatus", request.AttendanceStatus);
            }

            // ADDED: LongAbsent filtering
            if (!string.IsNullOrEmpty(request.LongAbsentOption) && request.LongAbsentOption != "All")
            {
                if (request.LongAbsentOption == "ExcludeLongAbsent")
                {
                    whereClause.Append(" AND ISNULL(LongAbsent, 0) = 0");
                }
                else if (request.LongAbsentOption == "OnlyLongAbsent")
                {
                    whereClause.Append(" AND LongAbsent = 1");
                }
            }

            // Apply search
            if (!string.IsNullOrEmpty(request.SearchValue))
            {
                whereClause.Append(@" AND (
                    EmployeeCode LIKE @Search OR 
                    EmployeeName LIKE @Search OR 
                    PunchNo LIKE @Search OR
                    Department LIKE @Search OR
                    Designation LIKE @Search
                )");
                parameters.Add("Search", $"%{request.SearchValue}%");
            }

            baseQuery += whereClause.ToString();

            // Get total count
            var countQuery = $"SELECT COUNT(*) {baseQuery}";
            var totalRecords = await connection.QuerySingleAsync<int>(countQuery, parameters);

            // Apply sorting
            var orderBy = " ORDER BY ";
            if (request.SortColumn >= 0 && request.SortColumn < request.Columns.Count)
            {
                var columnName = request.Columns[request.SortColumn];
                orderBy += GetSortColumn(columnName) + " " + request.SortDirection;
            }
            else
            {
                orderBy += "AttendanceDate DESC, EmployeeCode";
            }

            // Apply pagination
            var dataQuery = $@"
                SELECT 
                    CompanyName,
                    EmployeeCode,
                    PunchNo,
                    EmployeeName,
                    Department,
                    Designation,
                    Category,
                    Section,
                    AttendanceDate,
                    FirstPunchTime,
                    AttendanceStatus,
                    PerDayCTC,
                    ProcessedDate,
                    ISNULL(LongAbsent, 0) as LongAbsent
                {baseQuery}
                {orderBy}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            parameters.Add("Skip", request.Start);
            parameters.Add("Take", request.Length);

            var data = await connection.QueryAsync<AttendanceSummaryData>(dataQuery, parameters);

            return new DataTableResponse<AttendanceSummaryData>
            {
                Data = data.ToList(),
                TotalRecords = totalRecords,
                FilteredRecords = totalRecords
            };
        }

        public async Task<List<AttendanceSummaryData>> GetAttendanceForExportAsync(AttendanceExportRequest request)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT 
                    CompanyName,
                    EmployeeCode,
                    PunchNo,
                    EmployeeName,
                    Department,
                    Designation,
                    Category,
                    Section,
                    AttendanceDate,
                    FirstPunchTime,
                    AttendanceStatus,
                    PerDayCTC,
                    ProcessedDate,
                    ISNULL(LongAbsent, 0) as LongAbsent
                FROM vw_AttendanceSummaryReport
                WHERE AttendanceDate BETWEEN @FromDate AND @ToDate";

            var parameters = new DynamicParameters();
            parameters.Add("FromDate", request.FromDate);
            parameters.Add("ToDate", request.ToDate);

            if (request.CompanyCode > 0)
            {
                query += " AND CompanyCode = @CompanyCode";
                parameters.Add("CompanyCode", request.CompanyCode);
            }

            if (!string.IsNullOrEmpty(request.Department))
            {
                query += " AND Department = @Department";
                parameters.Add("Department", request.Department);
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                query += " AND Category = @Category";
                parameters.Add("Category", request.Category);
            }

            if (!string.IsNullOrEmpty(request.Designation))
            {
                query += " AND Designation = @Designation";
                parameters.Add("Designation", request.Designation);
            }

            if (!string.IsNullOrEmpty(request.AttendanceStatus) && request.AttendanceStatus != "All")
            {
                query += " AND AttendanceStatus = @AttendanceStatus";
                parameters.Add("AttendanceStatus", request.AttendanceStatus);
            }

            // ADDED: LongAbsent filtering for export
            if (!string.IsNullOrEmpty(request.LongAbsentOption) && request.LongAbsentOption != "All")
            {
                if (request.LongAbsentOption == "ExcludeLongAbsent")
                {
                    query += " AND ISNULL(LongAbsent, 0) = 0";
                }
                else if (request.LongAbsentOption == "OnlyLongAbsent")
                {
                    query += " AND LongAbsent = 1";
                }
            }

            query += " ORDER BY AttendanceDate DESC, EmployeeCode";

            var result = await connection.QueryAsync<AttendanceSummaryData>(query, parameters);
            return result.ToList();
        }

        public async Task<List<string>> GetDepartmentsAsync(int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Department 
                FROM vw_AttendanceSummaryReport
                WHERE Department IS NOT NULL";

            if (companyCode > 0)
            {
                query += " AND CompanyCode = @CompanyCode";
            }

            query += " ORDER BY Department";

            var result = await connection.QueryAsync<string>(query, new { CompanyCode = companyCode });
            return result.ToList();
        }

        public async Task<List<string>> GetCategoriesAsync(int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Category 
                FROM vw_AttendanceSummaryReport
                WHERE Category IS NOT NULL";

            if (companyCode > 0)
            {
                query += " AND CompanyCode = @CompanyCode";
            }

            query += " ORDER BY Category";

            var result = await connection.QueryAsync<string>(query, new { CompanyCode = companyCode });
            return result.ToList();
        }

        public async Task<List<string>> GetDesignationsAsync(int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Designation 
                FROM vw_AttendanceSummaryReport
                WHERE Designation IS NOT NULL";

            if (companyCode > 0)
            {
                query += " AND CompanyCode = @CompanyCode";
            }

            query += " ORDER BY Designation";

            var result = await connection.QueryAsync<string>(query, new { CompanyCode = companyCode });
            return result.ToList();
        }

        public async Task<List<string>> GetDesignationsByDepartmentAsync(string department, int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Designation 
                FROM vw_AttendanceSummaryReport
                WHERE Designation IS NOT NULL
                AND Department = @Department";

            if (companyCode > 0)
            {
                query += " AND CompanyCode = @CompanyCode";
            }

            query += " ORDER BY Designation";

            var result = await connection.QueryAsync<string>(query, new { Department = department, CompanyCode = companyCode });
            return result.ToList();
        }

        private string GetSortColumn(string columnName)
        {
            return columnName switch
            {
                "CompanyName" => "CompanyName",
                "EmployeeCode" => "EmployeeCode",
                "PunchNo" => "PunchNo",
                "EmployeeName" => "EmployeeName",
                "Department" => "Department",
                "Designation" => "Designation",
                "Category" => "Category",
                "Section" => "Section",
                "AttendanceDate" => "AttendanceDate",
                "FirstPunchTime" => "FirstPunchTime",
                "AttendanceStatus" => "AttendanceStatus",
                "PerDayCTC" => "PerDayCTC",
                "LongAbsent" => "LongAbsent", // ADDED
                _ => "AttendanceDate"
            };
        }
    }
}