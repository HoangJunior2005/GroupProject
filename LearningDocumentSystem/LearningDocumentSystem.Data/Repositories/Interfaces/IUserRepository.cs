using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetWithRolesAsync(int userId);
        Task<IEnumerable<User>> GetAllWithRolesAsync();
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
        Task<Dictionary<(int Year, int Month), int>> GetMonthlyRegistrationsAsync(DateTime sinceDate);
    }

    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByNameAsync(string roleName);
    }

    public interface IUserRoleRepository : IGenericRepository<UserRole>
    {
        Task<bool> HasRoleAsync(int userId, string roleName);
        Task AssignRoleAsync(int userId, int roleId, DateTime? expiresAt = null);
        Task RemoveRoleAsync(int userId, int roleId);
        /// <summary>
        /// Trả về ngày hết hạn sớm nhất của các role trả phí (Plus/Pro) của user. Null nếu không có.
        /// </summary>
        Task<DateTime?> GetPaidRoleExpiryAsync(int userId, IEnumerable<string> paidRoleNames);
        /// <summary>
        /// Trả về tên các role trong danh sách cho trước mà user đang có và chưa hết hạn.
        /// </summary>
        Task<IEnumerable<string>> GetActiveRoleNamesAsync(int userId, IEnumerable<string> roleNames);
    }
}
