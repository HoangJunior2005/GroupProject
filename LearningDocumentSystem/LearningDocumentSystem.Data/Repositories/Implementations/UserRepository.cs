using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByUsernameAsync(string username)
            => await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => (u.Username == username || u.Email == username) && u.IsActive);

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetWithRolesAsync(int userId)
            => await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

        public async Task<IEnumerable<User>> GetAllWithRolesAsync()
            => await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ToListAsync();

        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
            => await _context.UserRoles
                .Where(ur => ur.UserID == userId)
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();

        public async Task<bool> IsUsernameExistsAsync(string username)
            => await _context.Users.AnyAsync(u => u.Username == username);

        public async Task<bool> IsEmailExistsAsync(string email)
            => await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<Dictionary<(int Year, int Month), int>> GetMonthlyRegistrationsAsync(DateTime sinceDate)
        {
            return await _context.Users
                .Where(u => u.CreatedAt >= sinceDate)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToDictionaryAsync(x => (x.Year, x.Month), x => x.Count);
        }
    }

    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context) { }

        public async Task<Role?> GetByNameAsync(string roleName)
            => await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
    }

    public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
    {
        public UserRoleRepository(AppDbContext context) : base(context) { }

        public async Task<bool> HasRoleAsync(int userId, string roleName)
            => await _context.UserRoles
                .AnyAsync(ur => ur.UserID == userId && ur.Role.RoleName == roleName);

        public async Task AssignRoleAsync(int userId, int roleId, DateTime? expiresAt = null)
        {
            var existing = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserID == userId && ur.RoleID == roleId);
            if (existing != null)
            {
                // Cập nhật ngày hết hạn (gia hạn gói)
                existing.ExpiresAt = expiresAt;
            }
            else
            {
                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserID = userId,
                    RoleID = roleId,
                    ExpiresAt = expiresAt
                });
            }
        }

        public async Task RemoveRoleAsync(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserID == userId && ur.RoleID == roleId);
            if (userRole != null)
                _context.UserRoles.Remove(userRole);
        }

        public async Task<DateTime?> GetPaidRoleExpiryAsync(int userId, IEnumerable<string> paidRoleNames)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserID == userId
                    && paidRoleNames.Contains(ur.Role.RoleName)
                    && ur.ExpiresAt != null)
                .OrderByDescending(ur => ur.ExpiresAt)
                .Select(ur => ur.ExpiresAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<string>> GetActiveRoleNamesAsync(int userId, IEnumerable<string> roleNames)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserID == userId
                    && roleNames.Contains(ur.Role.RoleName)
                    && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();
        }
    }
}
