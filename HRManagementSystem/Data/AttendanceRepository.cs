using Dapper;
using HRManagementSystem.Models;
using Microsoft.Data.SqlClient;

namespace HRManagementSystem.Data
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly string _hrConnectionString;
        private readonly string _attendanceConnectionString;
        private readonly string _newAttendanceConnectionString;

        public AttendanceRepository(IConfiguration configuration)
        {
            _hrConnectionString = configuration.GetConnectionString("DefaultConnection");
            _attendanceConnectionString = configuration.GetConnectionString("AttendanceConnection");
            _newAttendanceConnectionString = configuration.GetConnectionString("NewAttendanceConnection");
        }

        public async Task<bool> ProcessDailyAttendanceAsync(DateTime processDate)
        {
            try
            {
                using var hrConnection = new SqlConnection(_hrConnectionString);
                using var attendanceConnection = new SqlConnection(_attendanceConnectionString);
                using var newAttendanceConnection = new SqlConnection(_newAttendanceConnectionString);

                // Get first punch of each employee for the process date
                var firstPunchSql = @"
            WITH FirstPunch AS (
                SELECT Employeecode, 
                       MIN(LogDateTime) as FirstPunchDateTime,
                       MIN(LogTime) as FirstPunchTime
                FROM Parellellogs 
                WHERE LogDate = @ProcessDate 
                GROUP BY Employeecode
            )
            SELECT Employeecode, FirstPunchDateTime, FirstPunchTime
            FROM FirstPunch";

                var firstPunches = await attendanceConnection.QueryAsync(firstPunchSql, new { ProcessDate = processDate.Date });

                // Get all employees from HR master - INCLUDING LONGABSENT
                var employeesSql = @"
            SELECT CompanyCode, EmployeeCode, EmployeeName, Punchno, Dept, Desig, Category,
                   ISNULL(MainSection, 'OTHERS') as MainSection,
                   ISNULL(PerDayCTC, 0) as PerDayCTC,
                   ISNULL(LongAbsent, 0) as LongAbsent
            FROM vw_AttendanceEmployeeMaster 
            WHERE EmployeeStatus = 'ACTIVE'";

                var employees = await hrConnection.QueryAsync<Employee>(employeesSql);

                // Process each employee individually with error handling
                var processedCount = 0;
                var errorCount = 0;

                foreach (var employee in employees.GroupBy(e => new { e.CompanyCode, e.EmployeeCode }).Select(g => g.First()))
                {
                    try
                    {
                        var firstPunch = firstPunches.FirstOrDefault(fp => fp.Employeecode == employee.PunchNo);

                        // Check if record already exists
                        var existsQuery = @"
                    SELECT COUNT(*) FROM DailyAttendance 
                    WHERE CompanyCode = @CompanyCode 
                      AND EmployeeCode = @EmployeeCode 
                      AND AttendanceDate = @AttendanceDate";

                        var exists = await newAttendanceConnection.QuerySingleAsync<int>(existsQuery, new
                        {
                            CompanyCode = employee.CompanyCode,
                            EmployeeCode = employee.EmployeeCode,
                            AttendanceDate = processDate.Date
                        });

                        if (exists > 0)
                        {
                            // Update existing record - INCLUDING LONGABSENT
                            var updateSql = @"
                        UPDATE DailyAttendance 
                        SET FirstPunchTime = @FirstPunchTime, 
                            AttendanceStatus = @AttendanceStatus, 
                            Designation = @Designation,
                            Category = @Category,
                            Section = @Section,
                            PerDayCTC = @PerDayCTC,
                            LongAbsent = @LongAbsent,
                            LastUpdated = @LastUpdated
                        WHERE CompanyCode = @CompanyCode 
                          AND EmployeeCode = @EmployeeCode 
                          AND AttendanceDate = @AttendanceDate";

                            await newAttendanceConnection.ExecuteAsync(updateSql, new
                            {
                                CompanyCode = employee.CompanyCode,
                                EmployeeCode = employee.EmployeeCode,
                                AttendanceDate = processDate.Date,
                                FirstPunchTime = firstPunch?.FirstPunchTime,
                                AttendanceStatus = firstPunch != null ? "Present" : "Absent",
                                Designation = employee.Desig,
                                Category = employee.Category,
                                Section = employee.MainSection,
                                PerDayCTC = employee.PerDayCTC,
                                LongAbsent = employee.LongAbsent,
                                LastUpdated = DateTime.Now
                            });
                        }
                        else
                        {
                            // Insert new record - INCLUDING LONGABSENT
                            var insertSql = @"
                        INSERT INTO DailyAttendance (CompanyCode, EmployeeCode, PunchNo, EmployeeName, 
                                                   Department, Designation, Category, Section, PerDayCTC, LongAbsent, AttendanceDate, FirstPunchTime, AttendanceStatus, LastUpdated)
                        VALUES (@CompanyCode, @EmployeeCode, @PunchNo, @EmployeeName, 
                                @Department, @Designation, @Category, @Section, @PerDayCTC, @LongAbsent, @AttendanceDate, @FirstPunchTime, @AttendanceStatus, @LastUpdated)";

                            await newAttendanceConnection.ExecuteAsync(insertSql, new
                            {
                                CompanyCode = employee.CompanyCode,
                                EmployeeCode = employee.EmployeeCode,
                                PunchNo = employee.PunchNo,
                                EmployeeName = employee.EmployeeName,
                                Department = employee.Dept,
                                Designation = employee.Desig,
                                Category = employee.Category,
                                Section = employee.MainSection,
                                PerDayCTC = employee.PerDayCTC,
                                LongAbsent = employee.LongAbsent,
                                AttendanceDate = processDate.Date,
                                FirstPunchTime = firstPunch?.FirstPunchTime,
                                AttendanceStatus = firstPunch != null ? "Present" : "Absent",
                                LastUpdated = DateTime.Now
                            });
                        }

                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing employee {employee.EmployeeCode}: {ex.Message}");
                        errorCount++;
                    }
                }

                Console.WriteLine($"Processing completed: {processedCount} successful, {errorCount} errors");
                return errorCount == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing daily attendance: {ex.Message}");
                return false;
            }
        }

        public async Task<AttendanceReportViewModel> GetDailyAttendanceReportAsync(DateTime reportDate, int companyCode)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString);

            var sql = @"
                        WITH HierarchyAttendance AS (
                            SELECT 
                                COALESCE(dh.ParentDesignation, da.Department, 'Unassigned Department') as ParentDesignation,
                                COALESCE(dh.SubDesignation, da.Designation, 'Unassigned Designation') as SubDesignation,
                                COALESCE(da.Category, 'Unknown') as Category,
                                da.AttendanceStatus,
                                COUNT(*) as EmployeeCount
                            FROM DailyAttendance da
                            LEFT JOIN DesignationHierarchy dh ON dh.SubDesignation = da.Designation 
                                                              AND dh.ParentDesignation = da.Department
                                                              AND dh.IsActive = 1
                            WHERE da.AttendanceDate = @ReportDate
                                  AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
                                  AND ISNULL(da.LongAbsent, 0) = 0
                            GROUP BY COALESCE(dh.ParentDesignation, da.Department, 'Unassigned Department'), 
                                     COALESCE(dh.SubDesignation, da.Designation, 'Unassigned Designation'),
                                     COALESCE(da.Category, 'Unknown'),
                                     da.AttendanceStatus
                        )
                        SELECT 
                            ha.ParentDesignation, 
                            ha.SubDesignation,
                            SUM(CASE WHEN ha.AttendanceStatus = 'Present' THEN ha.EmployeeCount ELSE 0 END) as Present,
                            SUM(CASE WHEN ha.AttendanceStatus = 'Absent' THEN ha.EmployeeCount ELSE 0 END) as Absent,
                            SUM(ha.EmployeeCount) as Total,
                            -- Category breakdowns (present only)
                            SUM(CASE WHEN ha.Category = 'Worker' AND ha.AttendanceStatus = 'Present' THEN ha.EmployeeCount ELSE 0 END) as WorkerPresent,
                            SUM(CASE WHEN ha.Category = 'Staff' AND ha.AttendanceStatus = 'Present' THEN ha.EmployeeCount ELSE 0 END) as StaffPresent,
                            SUM(CASE WHEN ha.Category = 'Officer' AND ha.AttendanceStatus = 'Present' THEN ha.EmployeeCount ELSE 0 END) as OfficerPresent,
                            SUM(CASE WHEN ha.Category = 'Manager' AND ha.AttendanceStatus = 'Present' THEN ha.EmployeeCount ELSE 0 END) as ManagerPresent,
                            SUM(CASE WHEN ha.Category = 'Executive' AND ha.AttendanceStatus = 'Present' THEN ha.EmployeeCount ELSE 0 END) as ExecutivePresent,
                            SUM(CASE WHEN ha.Category NOT IN ('Worker', 'Staff', 'Officer', 'Manager', 'Executive') AND ha.AttendanceStatus = 'Present' THEN ha.EmployeeCount ELSE 0 END) as OtherPresent
                        FROM HierarchyAttendance ha
                        GROUP BY ha.ParentDesignation, ha.SubDesignation
                        ORDER BY ha.ParentDesignation, ha.SubDesignation";

            var result = await connection.QueryAsync<DailyAttendanceData>(sql, new { ReportDate = reportDate.Date, CompanyCode = companyCode });

            var reportData = result.GroupBy(r => r.ParentDesignation)
                .Select(g => new AttendanceByDesignation
                {
                    ParentDesignation = g.Key,
                    SubDesignations = g.Select(s => new AttendanceBySubDesignation
                    {
                        SubDesignation = s.SubDesignation,
                        Present = s.Present,
                        Absent = s.Absent,
                        Total = s.Total,
                        // Keep your existing fields as they were
                        Attacher = 0,
                        Folder = 0,
                        Sticher = 0,
                        Others = s.Present,
                        // ADD the new category fields
                        WorkerPresent = s.WorkerPresent,
                        StaffPresent = s.StaffPresent,
                        OfficerPresent = s.OfficerPresent,
                        ManagerPresent = s.ManagerPresent,
                        ExecutivePresent = s.ExecutivePresent,
                        OtherPresent = s.OtherPresent
                    }).ToList(),
                    TotalEmployees = g.Sum(s => s.Total),
                    PresentEmployees = g.Sum(s => s.Present),
                    AbsentEmployees = g.Sum(s => s.Absent)
                }).ToList();

            return new AttendanceReportViewModel
            {
                AttendanceByDesignations = reportData,
                ReportDate = reportDate,
                TotalEmployees = reportData.Sum(r => r.TotalEmployees),
                PresentEmployees = reportData.Sum(r => r.PresentEmployees),
                AbsentEmployees = reportData.Sum(r => r.AbsentEmployees)
            };
        }

        public async Task<List<CompanyAttendanceStats>> GetCompaniesWithAttendanceAsync(DateTime reportDate)
        {
            using var connection = new SqlConnection(_newAttendanceConnectionString); // Read from Server 3

            var sql = @"
                        SELECT 
                            da.CompanyCode,
                            COUNT(*) as TotalEmployees,
                            SUM(CASE WHEN da.AttendanceStatus = 'Present' THEN 1 ELSE 0 END) as PresentEmployees,
                            SUM(CASE WHEN da.AttendanceStatus = 'Absent' THEN 1 ELSE 0 END) as AbsentEmployees
                        FROM DailyAttendance da
                        WHERE da.AttendanceDate = @ReportDate
                          AND ISNULL(da.LongAbsent, 0) = 0
                        GROUP BY da.CompanyCode";

            var result = await connection.QueryAsync<CompanyAttendanceStats>(sql, new { ReportDate = reportDate.Date });
            return result.ToList();
        }

        #region 'Dashboard attendance report'


        #endregion

        #region 'department attendance report'
        // Add this method to your AttendanceRepository class

        public async Task<DepartmentAttendanceViewModel> GetDepartmentAttendanceReportAsyncold(DateTime reportDate, int companyCode, string department = "ALL")
        {
            using var hrConnection = new SqlConnection(_hrConnectionString);
            using var newAttendanceConnection = new SqlConnection(_newAttendanceConnectionString);

            // Get employee master data from existing view - EXCLUDING LONGABSENT = 1
            var employeeDataSql = @"
        SELECT 
            CompanyCode, 
            EmployeeCode, 
            EmployeeName, 
            Punchno, 
            Dept as Department, 
            Desig as Designation, 
            Category,
            ISNULL(MainSection, 'OTHERS') as MainSection
        FROM vw_AttendanceEmployeeMaster 
        WHERE EmployeeStatus = 'ACTIVE' and CategoryCode not in (1,10) 
        AND ISNULL(LongAbsent, 0) = 0
        AND (@CompanyCode = 0 OR CompanyCode = @CompanyCode)
        AND (@Department = 'ALL' OR Dept = @Department)";

            var employeeData = await hrConnection.QueryAsync<dynamic>(employeeDataSql, new { CompanyCode = companyCode, Department = department });

            // Get attendance data for the report date - EXCLUDING LONGABSENT = 1
            var attendanceDataSql = @"
        SELECT 
            EmployeeCode,
            CompanyCode,
            AttendanceStatus
        FROM DailyAttendance
        WHERE AttendanceDate = @ReportDate and Category not in ('STAFF','CONSULTANT')
        AND ISNULL(LongAbsent, 0) = 0
        AND (@CompanyCode = 0 OR CompanyCode = @CompanyCode)";

            var attendanceData = await newAttendanceConnection.QueryAsync<dynamic>(attendanceDataSql,
                new { ReportDate = reportDate.Date, CompanyCode = companyCode });

            // Get available departments for dropdown - EXCLUDING LONGABSENT = 1
            var availableDepartmentsSql = @"
        SELECT DISTINCT Dept 
        FROM vw_AttendanceEmployeeMaster 
        WHERE EmployeeStatus = 'ACTIVE' 
        AND Dept IS NOT NULL and CategoryCode not in (1,10)
        AND ISNULL(LongAbsent, 0) = 0
        AND (@CompanyCode = 0 OR CompanyCode = @CompanyCode)
        ORDER BY Dept";

            var availableDepartments = await hrConnection.QueryAsync<string>(availableDepartmentsSql, new { CompanyCode = companyCode });

            // Join employee data with attendance data
            var combinedData = from emp in employeeData
                               join att in attendanceData on emp.EmployeeCode equals att.EmployeeCode into attGroup
                               from att in attGroup.DefaultIfEmpty()
                               select new
                               {
                                   EmployeeCode = emp.EmployeeCode,
                                   EmployeeName = emp.EmployeeName,
                                   Department = emp.Department ?? "Unknown Department",
                                   MainSection = emp.MainSection ?? "OTHERS",
                                   Designation = emp.Designation ?? "Unknown Designation",
                                   AttendanceStatus = att?.AttendanceStatus ?? "Absent",
                                   CompanyCode = emp.CompanyCode
                               };

            // Group and organize data
            var departmentGroups = combinedData.GroupBy(x => x.Department).ToList();
            var departments = new List<DepartmentAttendanceData>();
            var grandTotals = new DepartmentTotals();

            foreach (var deptGroup in departmentGroups)
            {
                var departmentData = new DepartmentAttendanceData
                {
                    DepartmentName = deptGroup.Key,
                    MainSections = new List<MainSectionData>()
                };

                var mainSectionGroups = deptGroup.GroupBy(x => x.MainSection);

                foreach (var mainSectionGroup in mainSectionGroups)
                {
                    var mainSection = new MainSectionData
                    {
                        MainSectionName = mainSectionGroup.Key,
                        NOWData = new List<NOWAttendanceData>()
                    };

                    // Group by designation as NOW (Nature of Work)
                    var nowGroups = mainSectionGroup.GroupBy(x => x.Designation);

                    foreach (var nowGroup in nowGroups)
                    {
                        var presentCount = nowGroup.Count(x => x.AttendanceStatus == "Present");
                        var absentCount = nowGroup.Count(x => x.AttendanceStatus == "Absent");

                        mainSection.NOWData.Add(new NOWAttendanceData
                        {
                            NOWName = nowGroup.Key,
                            Present = presentCount,
                            Absent = absentCount,
                            Total = presentCount + absentCount
                        });
                    }

                    mainSection.TotalPresent = mainSection.NOWData.Sum(x => x.Present);
                    mainSection.TotalAbsent = mainSection.NOWData.Sum(x => x.Absent);
                    mainSection.Total = mainSection.TotalPresent + mainSection.TotalAbsent;

                    departmentData.MainSections.Add(mainSection);
                }

                // Calculate department totals
                CalculateDepartmentTotals(departmentData);

                // Add to grand totals
                AddToGrandTotals(grandTotals, departmentData.DepartmentTotals);

                departments.Add(departmentData);
            }

            return new DepartmentAttendanceViewModel
            {
                ReportDate = reportDate,
                SelectedCompanyCode = companyCode,
                SelectedDepartment = department,
                Departments = departments,
                GrandTotals = grandTotals,
                AvailableDepartments = availableDepartments.ToList()
            };
        }

        private void CalculateDepartmentTotals(DepartmentAttendanceData department)
        {
            department.DepartmentTotals = new DepartmentTotals();

            foreach (var mainSection in department.MainSections)
            {
                var present = mainSection.TotalPresent;
                var absent = mainSection.TotalAbsent;
                var total = mainSection.Total;

                // Clean the MainSectionName to handle hidden characters
                var cleanMainSectionName = (mainSection.MainSectionName ?? "").Trim().ToUpper();

                // TEMPORARY DEBUG - Add this to see what's actually happening
                Console.WriteLine($"DEBUG: MainSection = '{cleanMainSectionName}' (Length: {cleanMainSectionName.Length})");

                // Use if-else instead of switch for better control
                if (cleanMainSectionName == "ATTACHER")
                {
                    department.DepartmentTotals.AttacherPresent = present;
                    department.DepartmentTotals.AttacherAbsent = absent;
                    department.DepartmentTotals.AttacherTotal = total;
                    Console.WriteLine($"DEBUG: ATTACHER matched - Present: {present}, Absent: {absent}");
                }
                else if (cleanMainSectionName == "FOLDER")
                {
                    department.DepartmentTotals.FolderPresent = present;
                    department.DepartmentTotals.FolderAbsent = absent;
                    department.DepartmentTotals.FolderTotal = total;
                }
                else if (cleanMainSectionName == "OTHERS")
                {
                    department.DepartmentTotals.OthersPresent = present;
                    department.DepartmentTotals.OthersAbsent = absent;
                    department.DepartmentTotals.OthersTotal = total;
                }
                else if (cleanMainSectionName == "SKIVER")
                {
                    department.DepartmentTotals.SkiverPresent = present;
                    department.DepartmentTotals.SkiverAbsent = absent;
                    department.DepartmentTotals.SkiverTotal = total;
                }
                else if (cleanMainSectionName == "STITCHER")
                {
                    department.DepartmentTotals.StitcherPresent = present;
                    department.DepartmentTotals.StitcherAbsent = absent;
                    department.DepartmentTotals.StitcherTotal = total;
                }
                else
                {
                    // Handle unknown sections - add to Others
                    Console.WriteLine($"DEBUG: Unknown section '{cleanMainSectionName}' - adding to Others");
                    department.DepartmentTotals.OthersPresent += present;
                    department.DepartmentTotals.OthersAbsent += absent;
                    department.DepartmentTotals.OthersTotal += total;
                }
            }

            // Calculate totals
            department.DepartmentTotals.TotalPresent = department.DepartmentTotals.AttacherPresent +
                                                      department.DepartmentTotals.FolderPresent +
                                                      department.DepartmentTotals.OthersPresent +
                                                      department.DepartmentTotals.SkiverPresent +
                                                      department.DepartmentTotals.StitcherPresent;

            department.DepartmentTotals.TotalAbsent = department.DepartmentTotals.AttacherAbsent +
                                                     department.DepartmentTotals.FolderAbsent +
                                                     department.DepartmentTotals.OthersAbsent +
                                                     department.DepartmentTotals.SkiverAbsent +
                                                     department.DepartmentTotals.StitcherAbsent;

            department.DepartmentTotals.GrandTotal = department.DepartmentTotals.TotalPresent + department.DepartmentTotals.TotalAbsent;
        }

        private void AddToGrandTotals(DepartmentTotals grandTotals, DepartmentTotals departmentTotals)
        {
            grandTotals.AttacherPresent += departmentTotals.AttacherPresent;
            grandTotals.AttacherAbsent += departmentTotals.AttacherAbsent;
            grandTotals.AttacherTotal += departmentTotals.AttacherTotal;

            grandTotals.FolderPresent += departmentTotals.FolderPresent;
            grandTotals.FolderAbsent += departmentTotals.FolderAbsent;
            grandTotals.FolderTotal += departmentTotals.FolderTotal;

            grandTotals.OthersPresent += departmentTotals.OthersPresent;
            grandTotals.OthersAbsent += departmentTotals.OthersAbsent;
            grandTotals.OthersTotal += departmentTotals.OthersTotal;

            grandTotals.SkiverPresent += departmentTotals.SkiverPresent;
            grandTotals.SkiverAbsent += departmentTotals.SkiverAbsent;
            grandTotals.SkiverTotal += departmentTotals.SkiverTotal;

            grandTotals.StitcherPresent += departmentTotals.StitcherPresent;
            grandTotals.StitcherAbsent += departmentTotals.StitcherAbsent;
            grandTotals.StitcherTotal += departmentTotals.StitcherTotal;

            grandTotals.TotalPresent += departmentTotals.TotalPresent;
            grandTotals.TotalAbsent += departmentTotals.TotalAbsent;
            grandTotals.GrandTotal += departmentTotals.GrandTotal;
        }
        #endregion

        public async Task<DepartmentAttendanceViewModel> GetDepartmentAttendanceReportAsync(DateTime reportDate, int companyCode, string department = "ALL")
        {
            using var newAttendanceConnection = new SqlConnection(_newAttendanceConnectionString);

            // Single optimized query that joins data at database level
            var mainDataSql = @"
        WITH EmployeeWithAttendance AS (
            SELECT 
                da.CompanyCode,
                da.EmployeeCode,
                da.EmployeeName,
                da.Department,
                da.Designation,
                da.Section as MainSection,
                da.AttendanceStatus
            FROM DailyAttendance da
            WHERE da.AttendanceDate = @ReportDate
              AND da.Category NOT IN ('STAFF','CONSULTANT')
              AND ISNULL(da.LongAbsent, 0) = 0
              AND (@CompanyCode = 0 OR da.CompanyCode = @CompanyCode)
              AND (@Department = 'ALL' OR da.Department = @Department)
        ),
        AggregatedData AS (
            SELECT 
                ISNULL(Department, 'Unknown Department') as Department,
                ISNULL(MainSection, 'OTHERS') as MainSection,
                ISNULL(Designation, 'Unknown Designation') as Designation,
                SUM(CASE WHEN AttendanceStatus = 'Present' THEN 1 ELSE 0 END) as PresentCount,
                SUM(CASE WHEN AttendanceStatus = 'Absent' THEN 1 ELSE 0 END) as AbsentCount,
                COUNT(*) as TotalCount
            FROM EmployeeWithAttendance
            GROUP BY Department, MainSection, Designation
        )
        SELECT 
            Department,
            MainSection,
            Designation,
            PresentCount,
            AbsentCount,
            TotalCount
        FROM AggregatedData
        ORDER BY Department, MainSection, Designation";

            // Get available departments
            var availableDepartmentsSql = @"
        SELECT DISTINCT Department 
        FROM DailyAttendance
        WHERE AttendanceDate = @ReportDate
          AND Department IS NOT NULL 
          AND Category NOT IN ('STAFF','CONSULTANT')
          AND ISNULL(LongAbsent, 0) = 0
          AND (@CompanyCode = 0 OR CompanyCode = @CompanyCode)
        ORDER BY Department";

            var queryParams = new
            {
                ReportDate = reportDate.Date,
                CompanyCode = companyCode,
                Department = department
            };

            // Execute queries sequentially to avoid MultipleActiveResultSets issue
            var aggregatedData = await newAttendanceConnection.QueryAsync<DepartmentSummaryResult>(mainDataSql, queryParams);
            var availableDepartments = await newAttendanceConnection.QueryAsync<string>(availableDepartmentsSql, queryParams);

            // Process the aggregated data efficiently
            var departments = ProcessAggregatedData(aggregatedData);
            var grandTotals = CalculateGrandTotals(departments);

            return new DepartmentAttendanceViewModel
            {
                ReportDate = reportDate,
                SelectedCompanyCode = companyCode,
                SelectedDepartment = department,
                Departments = departments,
                GrandTotals = grandTotals,
                AvailableDepartments = availableDepartments.ToList()
            };
        }

        // Add these helper methods to your AttendanceRepository class
        private List<DepartmentAttendanceData> ProcessAggregatedData(IEnumerable<DepartmentSummaryResult> aggregatedData)
        {
            var departments = new List<DepartmentAttendanceData>();

            var departmentGroups = aggregatedData.GroupBy(x => x.Department);

            foreach (var deptGroup in departmentGroups)
            {
                var departmentData = new DepartmentAttendanceData
                {
                    DepartmentName = deptGroup.Key,
                    MainSections = new List<MainSectionData>()
                };

                var mainSectionGroups = deptGroup.GroupBy(x => x.MainSection);

                foreach (var mainSectionGroup in mainSectionGroups)
                {
                    var mainSection = new MainSectionData
                    {
                        MainSectionName = mainSectionGroup.Key,
                        NOWData = mainSectionGroup.Select(x => new NOWAttendanceData
                        {
                            NOWName = x.Designation,
                            Present = x.PresentCount,
                            Absent = x.AbsentCount,
                            Total = x.TotalCount
                        }).ToList()
                    };

                    mainSection.TotalPresent = mainSection.NOWData.Sum(x => x.Present);
                    mainSection.TotalAbsent = mainSection.NOWData.Sum(x => x.Absent);
                    mainSection.Total = mainSection.TotalPresent + mainSection.TotalAbsent;

                    departmentData.MainSections.Add(mainSection);
                }

                CalculateDepartmentTotals(departmentData);
                departments.Add(departmentData);
            }

            return departments;
        }

        private DepartmentTotals CalculateGrandTotals(List<DepartmentAttendanceData> departments)
        {
            var grandTotals = new DepartmentTotals();
            foreach (var department in departments)
            {
                AddToGrandTotals(grandTotals, department.DepartmentTotals);
            }
            return grandTotals;
        }



    }
}