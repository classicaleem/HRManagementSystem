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

        public async Task<EmployeeStatusDataTableResponse<EmployeeStatusData>> GetEmployeeDataAsyncold(EmployeeStatusDataTableRequest request)
        {
            // Read from NewAttendanceConnection to display employee data
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            // Build base query - reading from DailyAttendance table (latest data)
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
                        c.CompanyName
                    FROM DailyAttendance da
                    LEFT JOIN Companies c ON da.CompanyCode = c.CompanyCode
                    WHERE da.AttendanceDate = (
                        SELECT MAX(AttendanceDate) 
                        FROM DailyAttendance da2 
                        WHERE da2.EmployeeCode = da.EmployeeCode
                    )
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
                    emp.Shift
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
            emp.Shift
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

        public async Task<EmployeeStatusUpdateResult> UpdateEmployeeStatusAsyncOld(EmployeeStatusBulkUpdateRequest request, string updatedBy)
        {
            // Update in DefaultConnection - NewEmployee table
            using var connection = new SqlConnection(_defaultConnectionString);

            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var result = new EmployeeStatusUpdateResult();
                var updatedCount = 0;

                foreach (var employeeCode in request.EmployeeCodes)
                {
                    try
                    {
                        // Build dynamic update query
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
                            //updateFields.Add("LastModified = @UpdatedDate");
                            //updateFields.Add("ModifiedBy = @UpdatedBy");

                            var updateSql = $@"
                                UPDATE NewEmployee 
                                SET {string.Join(", ", updateFields)}
                                WHERE EmployeeCode = @EmployeeCode";

                            var rowsAffected = await connection.ExecuteAsync(updateSql, parameters, transaction);

                            if (rowsAffected > 0)
                            {
                                updatedCount++;
                                // Log the update
                                await LogEmployeeStatusUpdateAsync(connection, transaction, employeeCode, request, updatedBy);
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

                transaction.Commit();

                result.Success = true;
                result.UpdatedCount = updatedCount;
                result.Message = $"Successfully updated {updatedCount} employee(s)";

                if (result.FailedEmployees.Any())
                {
                    result.Message += $". Failed to update {result.FailedEmployees.Count} employee(s)";
                }

                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new EmployeeStatusUpdateResult
                {
                    Success = false,
                    Message = $"Error updating employee status: {ex.Message}"
                };
            }
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
                                    await LogEmployeeStatusUpdateAsync(defaultConnection, defaultTransaction, employeeCode, request, updatedBy);
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
                var logSql = @"
                    INSERT INTO EmployeeStatusUpdateLog 
                    (EmployeeCode, UpdateType, OldValue, NewValue, UpdatedBy, UpdatedDate, Remarks)
                    VALUES (@EmployeeCode, @UpdateType, @OldValue, @NewValue, @UpdatedBy, @UpdatedDate, @Remarks)";

                if (request.LongAbsent.HasValue)
                {
                    await connection.ExecuteAsync(logSql, new
                    {
                        EmployeeCode = employeeCode,
                        UpdateType = "LongAbsent",
                        OldValue = (string)null,
                        NewValue = request.LongAbsent.Value ? "1" : "0",
                        UpdatedBy = updatedBy,
                        UpdatedDate = DateTime.Now,
                        Remarks = request.Remarks
                    }, transaction);
                }

                if (request.Layoff.HasValue)
                {
                    await connection.ExecuteAsync(logSql, new
                    {
                        EmployeeCode = employeeCode,
                        UpdateType = "Layoff",
                        OldValue = (string)null,
                        NewValue = request.Layoff.Value ? "1" : "0",
                        UpdatedBy = updatedBy,
                        UpdatedDate = DateTime.Now,
                        Remarks = request.Remarks
                    }, transaction);
                }

                if (!string.IsNullOrEmpty(request.Shift))
                {
                    await connection.ExecuteAsync(logSql, new
                    {
                        EmployeeCode = employeeCode,
                        UpdateType = "Shift",
                        OldValue = (string)null,
                        NewValue = request.Shift,
                        UpdatedBy = updatedBy,
                        UpdatedDate = DateTime.Now,
                        Remarks = request.Remarks
                    }, transaction);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the main update
                Console.WriteLine($"Failed to log status update for {employeeCode}: {ex.Message}");
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
    }
}