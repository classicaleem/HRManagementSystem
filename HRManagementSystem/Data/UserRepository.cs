using Dapper;
using HRManagementSystem.Models;
using Microsoft.Data.SqlClient;

namespace HRManagementSystem.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("NewAttendanceConnection");
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.Email, u.RoleId, r.RoleName, 
                       u.CompanyCode, c.CompanyName, u.IsActive, u.CreatedDate
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                LEFT JOIN Companies c ON u.CompanyCode = c.CompanyCode
                WHERE u.Username = @Username AND u.IsActive = 1";

            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });

            // CHANGED: Direct password comparison instead of BCrypt
            if (user != null && user.PasswordHash == password)
            {
                return user;
            }
            return null;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT u.UserId, u.Username, u.Email, u.RoleId, r.RoleName, 
                       u.CompanyCode, c.CompanyName, u.IsActive, u.CreatedDate
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                LEFT JOIN Companies c ON u.CompanyCode = c.CompanyCode
                WHERE u.UserId = @UserId";

            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
        }

        public async Task<List<User>> GetUsersAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT u.UserId, u.Username, u.Email, u.RoleId, r.RoleName, 
                       u.CompanyCode, c.CompanyName, u.IsActive, u.CreatedDate
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                LEFT JOIN Companies c ON u.CompanyCode = c.CompanyCode
                ORDER BY u.CreatedDate DESC";

            var result = await connection.QueryAsync<User>(sql);
            return result.ToList();
        }

        public async Task<int> CreateUserAsync(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                INSERT INTO Users (Username, PasswordHash, Email, RoleId, CompanyCode, IsActive)
                VALUES (@Username, @PasswordHash, @Email, @RoleId, @CompanyCode, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            // CHANGED: Store raw password instead of hashing
            // user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash); // REMOVED
            return await connection.QuerySingleAsync<int>(sql, user);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                UPDATE Users 
                SET Username = @Username, Email = @Email, RoleId = @RoleId, 
                    CompanyCode = @CompanyCode, IsActive = @IsActive
                WHERE UserId = @UserId";

            var rowsAffected = await connection.ExecuteAsync(sql, user);
            return rowsAffected > 0;
        }

        // NEW METHOD: Update user password
        public async Task<bool> UpdateUserPasswordAsync(int userId, string newPassword)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                UPDATE Users 
                SET PasswordHash = @Password, LastModified = GETDATE()
                WHERE UserId = @UserId";

            var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId, Password = newPassword });
            return rowsAffected > 0;
        }
    }
}