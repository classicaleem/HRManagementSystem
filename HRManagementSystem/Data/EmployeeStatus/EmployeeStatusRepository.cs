using Dapper;
using HRManagementSystem.Models.EmployeeStatus;
using Microsoft.Data.SqlClient;
using System.Text;

namespace HRManagementSystem.Repositories.EmployeeStatus
{
    public class EmployeeStatusRepository : IEmployeeStatusRepository
    {
        private readonly string _defaultConnectionString;
        private readonly string _newAttendanceConnectionString;

        public EmployeeStatusRepository(IConfiguration configuration)
        {
            _defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
            _newAttendanceConnectionString = configuration.GetConnectionString("NewAttendanceConnection");
        }
        public async Task<EmployeeStatusDataTableResponse<EmployeeStatusData>> GetEmployeeDataAsync(EmployeeStatusDataTableRequest request)
        {
            // Read from NewAttendanceConnection to display employee data
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            // Build base query - reading from DailyAttendance table for current date only
            var baseQuery = @"
                            FROM (
                                SELECT DISTINCT 
                                    da.CompanyCode,
                                    da.EmployeeCode,
                                    da.PunchNo,
                                    da.EmployeeName,
                                    da.Department,
                                    da.Designation,
                                    da.Category,
                                    da.Section,
                                    ISNULL(da.LongAbsent, 0) as LongAbsent,
                                    ISNULL(da.Layoff, 0) as Layoff,
                                    ISNULL(da.Shift, 'G') as Shift,
                                    da.FirstPunchTime as FirstPunchTime,
                                    da.AttendanceStatus as AttendanceStatus,
                                    c.CompanyName
                                FROM DailyAttendance da
                                LEFT JOIN Companies c ON da.CompanyCode = c.CompanyCode
                                WHERE da.AttendanceDate = CAST(GETDATE() AS DATE)
                            ) emp
                            WHERE 1=1";

            var parameters = new DynamicParameters();

            // Apply filters
            var whereClause = new StringBuilder();

            if (request.CompanyCode > 0)
            {
                whereClause.Append(" AND emp.CompanyCode = @CompanyCode");
                parameters.Add("CompanyCode", request.CompanyCode);
            }

            if (!string.IsNullOrEmpty(request.Department))
            {
                whereClause.Append(" AND emp.Department = @Department");
                parameters.Add("Department", request.Department);
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                whereClause.Append(" AND emp.Category = @Category");
                parameters.Add("Category", request.Category);
            }

            if (!string.IsNullOrEmpty(request.Designation))
            {
                whereClause.Append(" AND emp.Designation = @Designation");
                parameters.Add("Designation", request.Designation);
            }

            // Status filtering
            if (!string.IsNullOrEmpty(request.StatusFilter) && request.StatusFilter != "All")
            {
                switch (request.StatusFilter)
                {
                    case "LongAbsent":
                        whereClause.Append(" AND emp.LongAbsent = 1");
                        break;
                    case "Layoff":
                        whereClause.Append(" AND emp.Layoff = 1");
                        break;
                    case "Active":
                        whereClause.Append(" AND emp.LongAbsent = 0 AND emp.Layoff = 0");
                        break;
                }
            }

            // Apply search
            if (!string.IsNullOrEmpty(request.SearchValue))
            {
                whereClause.Append(@" AND (
                        emp.EmployeeCode LIKE @Search OR 
                        emp.EmployeeName LIKE @Search OR 
                        emp.PunchNo LIKE @Search OR
                        emp.Department LIKE @Search OR
                        emp.Designation LIKE @Search
                    )");
                parameters.Add("Search", $"%{request.SearchValue}%");
            }

            baseQuery += whereClause.ToString();

            // Get total count
            var countQuery = $"SELECT COUNT(*) {baseQuery}";
            var totalRecords = await connection.QuerySingleAsync<int>(countQuery, parameters);

            // Apply sorting
            var orderBy = " ORDER BY ";
            if (request.SortColumn >= 0 && request.SortColumn < request.Columns?.Count)
            {
                var columnName = request.Columns[request.SortColumn];
                orderBy += GetSortColumn(columnName) + " " + request.SortDirection;
            }
            else
            {
                orderBy += "emp.EmployeeName";
            }

            // Apply pagination
            var dataQuery = $@"
                                SELECT 
                                    ISNULL(emp.CompanyName, 'Unknown') as CompanyName,
                                    emp.EmployeeCode,
                                    emp.PunchNo,
                                    emp.EmployeeName,
                                    ISNULL(emp.Department, '') as Department,
                                    ISNULL(emp.Designation, '') as Designation,
                                    ISNULL(emp.Category, '') as Category,
                                    ISNULL(emp.Section, '') as Section,
                                    CAST(NULL AS DATETIME) as DateOfJoining,
                                    'ACTIVE' as EmployeeStatus,
                                    emp.LongAbsent,
                                    emp.Layoff,
                                    emp.Shift,
                                    CASE 
                                        WHEN emp.FirstPunchTime IS NOT NULL 
                                        THEN FORMAT(emp.FirstPunchTime, 'yyyy-MM-ddTHH:mm:ss')
                                        ELSE NULL 
                                    END as FirstPunchTime,
                                    ISNULL(emp.AttendanceStatus, 'Unknown') as AttendanceStatus
                                {baseQuery}
                                {orderBy}
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            parameters.Add("Skip", request.Start);
            parameters.Add("Take", request.Length);

            var data = await connection.QueryAsync<EmployeeStatusData>(dataQuery, parameters);

            return new EmployeeStatusDataTableResponse<EmployeeStatusData>
            {
                Data = data.ToList(),
                TotalRecords = totalRecords,
                FilteredRecords = totalRecords
            };
        }

        public async Task<EmployeeStatusUpdateResult> UpdateEmployeeStatusAsync(EmployeeStatusBulkUpdateRequest request, string updatedBy)
        {
            var result = new EmployeeStatusUpdateResult();
            var updatedCount = 0;

            // Update DefaultConnection first
            using (var defaultConnection = new SqlConnection(_defaultConnectionString))
            {
                await defaultConnection.OpenAsync();
                using var defaultTransaction = defaultConnection.BeginTransaction();

                try
                {
                    foreach (var employeeCode in request.EmployeeCodes)
                    {
                        try
                        {
                            // Build dynamic update query for NewEmployee table
                            var updateFields = new List<string>();
                            var parameters = new DynamicParameters();
                            parameters.Add("EmployeeCode", employeeCode);
                            parameters.Add("UpdatedBy", updatedBy);
                            parameters.Add("UpdatedDate", DateTime.Now);

                            if (request.LongAbsent.HasValue)
                            {
                                updateFields.Add("LongAbsent = @LongAbsent");
                                parameters.Add("LongAbsent", request.LongAbsent.Value ? 1 : 0);
                            }

                            if (request.Layoff.HasValue)
                            {
                                updateFields.Add("Layoff = @Layoff");
                                parameters.Add("Layoff", request.Layoff.Value ? 1 : 0);
                            }

                            if (!string.IsNullOrEmpty(request.Shift))
                            {
                                updateFields.Add("Shift = @Shift");
                                parameters.Add("Shift", request.Shift);
                            }

                            if (updateFields.Any())
                            {
                                var updateSql = $@"
                            UPDATE NewEmployee 
                            SET {string.Join(", ", updateFields)}
                            WHERE EmployeeCode = @EmployeeCode";

                                var rowsAffected = await defaultConnection.ExecuteAsync(updateSql, parameters, defaultTransaction);

                                if (rowsAffected > 0)
                                {
                                    updatedCount++;
                                    // Log the update
                                    // await LogEmployeeStatusUpdateAsync(defaultConnection, defaultTransaction, employeeCode, request, updatedBy);
                                }
                                else
                                {
                                    result.FailedEmployees.Add(employeeCode);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailedEmployees.Add($"{employeeCode}: {ex.Message}");
                        }
                    }

                    defaultTransaction.Commit();
                }
                catch (Exception ex)
                {
                    defaultTransaction.Rollback();
                    return new EmployeeStatusUpdateResult
                    {
                        Success = false,
                        Message = $"Error updating NewEmployee table: {ex.Message}"
                    };
                }
            }

            // Update NewAttendanceConnection for current date records
            using (var attendanceConnection = new SqlConnection(_newAttendanceConnectionString))
            {
                await attendanceConnection.OpenAsync();
                using var attendanceTransaction = attendanceConnection.BeginTransaction();

                try
                {
                    foreach (var employeeCode in request.EmployeeCodes.Where(ec => !result.FailedEmployees.Contains(ec)))
                    {
                        try
                        {
                            // Build dynamic update query for DailyAttendance table (current date only)
                            var updateFields = new List<string>();
                            var parameters = new DynamicParameters();
                            parameters.Add("EmployeeCode", employeeCode);
                            parameters.Add("CurrentDate", DateTime.Today);

                            if (request.LongAbsent.HasValue)
                            {
                                updateFields.Add("LongAbsent = @LongAbsent");
                                parameters.Add("LongAbsent", request.LongAbsent.Value ? 1 : 0);
                            }

                            if (request.Layoff.HasValue)
                            {
                                updateFields.Add("Layoff = @Layoff");
                                parameters.Add("Layoff", request.Layoff.Value ? 1 : 0);
                            }

                            if (!string.IsNullOrEmpty(request.Shift))
                            {
                                updateFields.Add("Shift = @Shift");
                                parameters.Add("Shift", request.Shift);
                            }

                            if (updateFields.Any())
                            {
                                var updateSql = $@"
                            UPDATE DailyAttendance 
                            SET {string.Join(", ", updateFields)}
                            WHERE EmployeeCode = @EmployeeCode 
                            AND AttendanceDate = @CurrentDate";

                                await attendanceConnection.ExecuteAsync(updateSql, parameters, attendanceTransaction);
                                // Log the update
                                await LogEmployeeStatusUpdateAsync(attendanceConnection, attendanceTransaction, employeeCode, request, updatedBy);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail the entire operation for attendance table updates
                            Console.WriteLine($"Warning: Failed to update DailyAttendance for {employeeCode}: {ex.Message}");
                        }
                    }

                    attendanceTransaction.Commit();
                }
                catch (Exception ex)
                {
                    attendanceTransaction.Rollback();
                    // Don't fail the entire operation, just log the warning
                    Console.WriteLine($"Warning: Error updating DailyAttendance table: {ex.Message}");
                }
            }

            result.Success = true;
            result.UpdatedCount = updatedCount;
            result.Message = $"Successfully updated {updatedCount} employee(s)";

            if (result.FailedEmployees.Any())
            {
                result.Message += $". Failed to update {result.FailedEmployees.Count} employee(s)";
            }

            return result;
        }

        private async Task LogEmployeeStatusUpdateAsync(SqlConnection connection, SqlTransaction transaction,
     string employeeCode, EmployeeStatusBulkUpdateRequest request, string updatedBy)
        {
            try
            {
                // First, get the current values for this employee
                var getCurrentValuesSql = @"
            SELECT TOP 1 
                ISNULL(LongAbsent, 0) as LongAbsent,
                ISNULL(Layoff, 0) as Layoff,
                ISNULL(Shift, 'G') as Shift
            FROM DailyAttendance 
            WHERE EmployeeCode = @EmployeeCode 
            AND AttendanceDate = CAST(GETDATE() AS DATE)";

                var currentValues = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    getCurrentValuesSql,
                    new { EmployeeCode = employeeCode },
                    transaction);

                var logSql = @"
            INSERT INTO EmployeeStatusUpdateLog 
            (EmployeeCode, UpdateType, OldValue, NewValue, UpdatedBy, UpdatedDate, Remarks)
            VALUES (@EmployeeCode, @UpdateType, @OldValue, @NewValue, @UpdatedBy, @UpdatedDate, @Remarks)";

                // Log LongAbsent changes
                if (request.LongAbsent.HasValue)
                {
                    var oldValue = currentValues?.LongAbsent?.ToString() ?? "0";
                    var newValue = request.LongAbsent.Value ? "1" : "0";

                    // Only log if there's actually a change
                    if (oldValue != newValue)
                    {
                        await connection.ExecuteAsync(logSql, new
                        {
                            EmployeeCode = employeeCode,
                            UpdateType = "LongAbsent",
                            OldValue = oldValue == "1" ? "Yes" : "No",
                            NewValue = newValue == "1" ? "Yes" : "No",
                            UpdatedBy = updatedBy,
                            UpdatedDate = DateTime.Now,
                            Remarks = request.Remarks ?? $"Bulk update: Long Absent changed from {(oldValue == "1" ? "Yes" : "No")} to {(newValue == "1" ? "Yes" : "No")}"
                        }, transaction);
                    }
                }

                // Log Layoff changes
                if (request.Layoff.HasValue)
                {
                    var oldValue = currentValues?.Layoff?.ToString() ?? "0";
                    var newValue = request.Layoff.Value ? "1" : "0";

                    // Only log if there's actually a change
                    if (oldValue != newValue)
                    {
                        await connection.ExecuteAsync(logSql, new
                        {
                            EmployeeCode = employeeCode,
                            UpdateType = "Layoff",
                            OldValue = oldValue == "1" ? "Yes" : "No",
                            NewValue = newValue == "1" ? "Yes" : "No",
                            UpdatedBy = updatedBy,
                            UpdatedDate = DateTime.Now,
                            Remarks = request.Remarks ?? $"Bulk update: Layoff changed from {(oldValue == "1" ? "Yes" : "No")} to {(newValue == "1" ? "Yes" : "No")}"
                        }, transaction);
                    }
                }

                // Log Shift changes
                if (!string.IsNullOrEmpty(request.Shift))
                {
                    var oldValue = currentValues?.Shift?.ToString() ?? "G";
                    var newValue = request.Shift;

                    // Only log if there's actually a change
                    if (oldValue != newValue)
                    {
                        await connection.ExecuteAsync(logSql, new
                        {
                            EmployeeCode = employeeCode,
                            UpdateType = "Shift",
                            OldValue = oldValue,
                            NewValue = newValue,
                            UpdatedBy = updatedBy,
                            UpdatedDate = DateTime.Now,
                            Remarks = request.Remarks ?? $"Bulk update: Shift changed from {oldValue} to {newValue}"
                        }, transaction);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the main update
                Console.WriteLine($"Failed to log status update for {employeeCode}: {ex.Message}");
                // Optionally, you could use a proper logging framework here:
                // _logger.LogWarning(ex, "Failed to log status update for employee {EmployeeCode}", employeeCode);
            }
        }
        public async Task<List<string>> GetDepartmentsAsync(int companyCode)
        {
            // Read from NewAttendanceConnection
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Department 
                FROM DailyAttendance
                WHERE Department IS NOT NULL 
                AND AttendanceDate = (SELECT MAX(AttendanceDate) FROM DailyAttendance)";

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
            // Read from NewAttendanceConnection
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Category 
                FROM DailyAttendance
                WHERE Category IS NOT NULL 
                AND AttendanceDate = (SELECT MAX(AttendanceDate) FROM DailyAttendance)";

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
            // Read from NewAttendanceConnection
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Designation 
                FROM DailyAttendance
                WHERE Designation IS NOT NULL 
                AND AttendanceDate = (SELECT MAX(AttendanceDate) FROM DailyAttendance)";

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
            // Read from NewAttendanceConnection
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var query = @"
                SELECT DISTINCT Designation 
                FROM DailyAttendance
                WHERE Designation IS NOT NULL
                AND Department = @Department
                AND AttendanceDate = (SELECT MAX(AttendanceDate) FROM DailyAttendance)";

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
            return columnName?.ToLower() switch
            {
                "companyname" => "emp.CompanyName",
                "employeecode" => "emp.EmployeeCode",
                "punchno" => "emp.PunchNo",
                "employeename" => "emp.EmployeeName",
                "department" => "emp.Department",
                "designation" => "emp.Designation",
                "category" => "emp.Category",
                "section" => "emp.Section",
                "firstpunchtime" => "emp.FirstPunchTime",
                "attendancestatus" => "emp.AttendanceStatus",
                "longabsent" => "emp.LongAbsent",
                "layoff" => "emp.Layoff",
                "shift" => "emp.Shift",
                _ => "emp.EmployeeName"
            };
        }
        private string GetSortColumnOLD(string columnName)
        {
            return columnName switch
            {
                "CompanyName" => "emp.CompanyName",
                "EmployeeCode" => "emp.EmployeeCode",
                "PunchNo" => "emp.PunchNo",
                "EmployeeName" => "emp.EmployeeName",
                "Department" => "emp.Department",
                "Designation" => "emp.Designation",
                "Category" => "emp.Category",
                "Section" => "emp.Section",
                "DateOfJoining" => "emp.EmployeeCode", // Fallback since we don't have this in DailyAttendance
                "LongAbsent" => "emp.LongAbsent",
                "Layoff" => "emp.Layoff",
                "Shift" => "emp.Shift",
                _ => "emp.EmployeeName"
            };
        }



        public async Task<EmployeeStatusDataTableResponse<EmployeeStatusData>> GetEmployeeDataForExportAsync(EmployeeStatusDataTableRequest request)
        {
            // Read from NewAttendanceConnection to display employee data
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            // Build base query - reading from DailyAttendance table for current date only
            var baseQuery = @"
FROM (
    SELECT DISTINCT 
        da.CompanyCode,
        da.EmployeeCode,
        da.PunchNo,
        da.EmployeeName,
        da.Department,
        da.Designation,
        da.Category,
        da.Section,
        ISNULL(da.LongAbsent, 0) as LongAbsent,
        ISNULL(da.Layoff, 0) as Layoff,
        ISNULL(da.Shift, 'G') as Shift,
        CASE 
            WHEN da.FirstPunchTime IS NOT NULL 
            THEN FORMAT(da.FirstPunchTime, 'yyyy-MM-ddTHH:mm:ss')
            ELSE NULL 
        END as FirstPunchTime,
        ISNULL(da.AttendanceStatus, 'Unknown') as AttendanceStatus,
        c.CompanyName
    FROM DailyAttendance da
    LEFT JOIN Companies c ON da.CompanyCode = c.CompanyCode
    WHERE da.AttendanceDate = CAST(GETDATE() AS DATE)
) emp
WHERE 1=1";

            var parameters = new DynamicParameters();

            // Apply filters
            var whereClause = new StringBuilder();

            if (request.CompanyCode > 0)
            {
                whereClause.Append(" AND emp.CompanyCode = @CompanyCode");
                parameters.Add("CompanyCode", request.CompanyCode);
            }

            if (!string.IsNullOrEmpty(request.Department))
            {
                whereClause.Append(" AND emp.Department = @Department");
                parameters.Add("Department", request.Department);
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                whereClause.Append(" AND emp.Category = @Category");
                parameters.Add("Category", request.Category);
            }

            if (!string.IsNullOrEmpty(request.Designation))
            {
                whereClause.Append(" AND emp.Designation = @Designation");
                parameters.Add("Designation", request.Designation);
            }

            // Status filtering
            if (!string.IsNullOrEmpty(request.StatusFilter) && request.StatusFilter != "All")
            {
                switch (request.StatusFilter)
                {
                    case "LongAbsent":
                        whereClause.Append(" AND emp.LongAbsent = 1");
                        break;
                    case "Layoff":
                        whereClause.Append(" AND emp.Layoff = 1");
                        break;
                    case "Active":
                        whereClause.Append(" AND emp.LongAbsent = 0 AND emp.Layoff = 0");
                        break;
                }
            }

            // Apply search
            if (!string.IsNullOrEmpty(request.SearchValue))
            {
                whereClause.Append(@" AND (
    emp.EmployeeCode LIKE @Search OR 
    emp.EmployeeName LIKE @Search OR 
    emp.PunchNo LIKE @Search OR
    emp.Department LIKE @Search OR
    emp.Designation LIKE @Search
)");
                parameters.Add("Search", $"%{request.SearchValue}%");
            }

            baseQuery += whereClause.ToString();

            // Get total count
            var countQuery = $"SELECT COUNT(*) {baseQuery}";
            var totalRecords = await connection.QuerySingleAsync<int>(countQuery, parameters);

            // For export, get all data without pagination
            var dataQuery = $@"
SELECT 
    ISNULL(emp.CompanyName, 'Unknown') as CompanyName,
    emp.EmployeeCode,
    emp.PunchNo,
    emp.EmployeeName,
    ISNULL(emp.Department, '') as Department,
    ISNULL(emp.Designation, '') as Designation,
    ISNULL(emp.Category, '') as Category,
    ISNULL(emp.Section, '') as Section,
    CAST(NULL AS DATETIME) as DateOfJoining,
    'ACTIVE' as EmployeeStatus,
    emp.LongAbsent,
    emp.Layoff,
    emp.Shift,
    emp.FirstPunchTime,
    emp.AttendanceStatus
{baseQuery}
ORDER BY emp.EmployeeName";

            var data = await connection.QueryAsync<EmployeeStatusData>(dataQuery, parameters);

            return new EmployeeStatusDataTableResponse<EmployeeStatusData>
            {
                Data = data.ToList(),
                TotalRecords = totalRecords,
                FilteredRecords = totalRecords
            };
        }
    }
}