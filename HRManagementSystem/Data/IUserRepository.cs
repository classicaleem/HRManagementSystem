using HRManagementSystem.Models;

namespace HRManagementSystem.Data
{
    public interface IUserRepository
    {
        Task<User> AuthenticateAsync(string username, string password);
        Task<User> GetUserByIdAsync(int userId);
        Task<List<User>> GetUsersAsync();
        Task<int> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> UpdateUserPasswordAsync(int userId, string newPassword); // NEW METHOD
    }
}