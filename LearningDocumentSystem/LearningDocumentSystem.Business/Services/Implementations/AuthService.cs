using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork uow, IMapper mapper, ILogger<AuthService> logger)
        {
            _uow    = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto?> LoginAsync(string username, string password)
        {
            var user = await _uow.Users.GetByUsernameAsync(username);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login failed for username: {Username}", username);
                return null;
            }

            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for username: {Username}", username);
                return null;
            }

            _logger.LogInformation("User {Username} logged in successfully.", username);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
            => !await _uow.Users.IsUsernameExistsAsync(username);

        public async Task<UserDto> RegisterAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Email và mật khẩu không được để trống.");
            }

            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
            {
                throw new ArgumentException("Định dạng email không hợp lệ.");
            }

            if (password.Length < 6)
            {
                throw new ArgumentException("Mật khẩu phải chứa ít nhất 6 ký tự.");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                // 1. Check whitelist
                var allowedEmail = await _uow.AllowedEmails.GetByEmailAsync(email);
                if (allowedEmail == null)
                {
                    throw new InvalidOperationException("Email này không nằm trong danh sách whitelist được phép đăng ký.");
                }

                // 2. Check if used
                if (allowedEmail.IsUsed)
                {
                    throw new InvalidOperationException("Email này đã được sử dụng để đăng ký tài khoản.");
                }

                // 3. Double check if email already exists in Users
                if (await _uow.Users.IsEmailExistsAsync(email))
                {
                    throw new InvalidOperationException("Email này đã tồn tại trong hệ thống.");
                }

                // 4. Determine Role (always Student for whitelisted registrations)
                string roleName = AppConstants.RoleStudent;

                var role = await _uow.Roles.GetByNameAsync(roleName);
                if (role == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy vai trò '{roleName}' trong hệ thống.");
                }

                // 5. Create user
                var newUser = new User
                {
                    Username = email, // email is unique and suitable as username
                    Email = email,
                    PasswordHash = PasswordHelper.HashPassword(password),
                    CanUpload = false,
                    FullName = email.Split('@')[0], // default name from email
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Users.AddAsync(newUser);
                await _uow.SaveChangesAsync(); // save to generate UserID

                // Assign role
                await _uow.UserRoles.AssignRoleAsync(newUser.UserID, role.RoleID);

                // Mark AllowedEmail as used
                allowedEmail.IsUsed = true;
                _uow.AllowedEmails.Update(allowedEmail);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                _logger.LogInformation("User with email {Email} successfully registered with role {Role}.", email, roleName);
                return _mapper.Map<UserDto>(newUser);
            }
            catch (Exception)
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }

    public class AdminUserService : IAdminUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminUserService> _logger;
        private readonly INotificationService _notificationService;

        public AdminUserService(IUnitOfWork uow, IMapper mapper, ILogger<AdminUserService> logger, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _uow.Users.GetAllWithRolesAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users)
                .Where(u => !u.Roles.Contains(AppConstants.RoleAdmin));
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _uow.Roles.GetAllAsync();
            return _mapper.Map<IEnumerable<RoleDto>>(roles)
                .Where(r => r.RoleName != PackageService.PlusCode && r.RoleName != PackageService.ProCode);
        }

        public async Task UpdateUserRolesAsync(int userId, IEnumerable<int> roleIds, bool canUpload)
        {
            var user = await _uow.Users.GetWithRolesAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role update.", userId);
                return;
            }

            if (user.UserRoles.Any(ur => ur.Role.RoleName == AppConstants.RoleAdmin))
            {
                _logger.LogWarning("Admin user {UserId} cannot be modified.", userId);
                return;
            }

            var singleRoleId = roleIds.Distinct().FirstOrDefault();
            if (singleRoleId == 0)
            {
                _logger.LogWarning("No role selected for user {UserId}.", userId);
                return;
            }

            var selectedRole = await _uow.Roles.GetByIdAsync(singleRoleId);
            if (selectedRole?.RoleName == AppConstants.RoleAdmin)
            {
                _logger.LogWarning("Admin role cannot be assigned to user {UserId}.", userId);
                return;
            }

            var existingRoleIds = user.UserRoles.Select(ur => ur.RoleID).ToHashSet();
            var packageRoleIds = user.UserRoles
                .Where(ur => ur.Role.RoleName == PackageService.PlusCode || ur.Role.RoleName == PackageService.ProCode)
                .Select(ur => ur.RoleID);
            var targetRoleIds = new HashSet<int>(packageRoleIds) { singleRoleId };

            foreach (var roleId in existingRoleIds.Except(targetRoleIds))
            {
                await _uow.UserRoles.RemoveRoleAsync(userId, roleId);
            }

            foreach (var roleId in targetRoleIds.Except(existingRoleIds))
            {
                await _uow.UserRoles.AssignRoleAsync(userId, roleId);
            }

            // Set upload permission. Only Teachers can have upload toggled.
            var isTeacher = selectedRole?.RoleName == AppConstants.RoleTeacher;
            user.CanUpload = isTeacher ? canUpload : false;

            await _uow.SaveChangesAsync();
            await _notificationService.SendNotificationAsync("UserChanged", new { action = "UpdateRoles", userId = userId });
        }

        public async Task<int> ImportAllowedEmailsAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File trống hoặc không hợp lệ.");
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
            {
                throw new ArgumentException("Chỉ chấp nhận file Excel định dạng .xlsx");
            }

            var emailsInFile = new List<string>();

            using (var stream = file.OpenReadStream())
            {
                var rows = stream.Query(useHeaderRow: true).ToList();
                foreach (var row in rows)
                {
                    var dict = row as IDictionary<string, object>;
                    if (dict != null)
                    {
                        var emailKey = dict.Keys.FirstOrDefault(k => k.Equals("Email", StringComparison.OrdinalIgnoreCase));
                        if (emailKey != null)
                        {
                            var emailVal = dict[emailKey]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(emailVal))
                            {
                                emailsInFile.Add(emailVal);
                            }
                        }
                    }
                    else
                    {
                        var val = row?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(val))
                        {
                            emailsInFile.Add(val);
                        }
                    }
                }
            }

            if (!emailsInFile.Any())
            {
                throw new ArgumentException("Không tìm thấy dữ liệu email trong file Excel.");
            }

            var validEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            foreach (var email in emailsInFile)
            {
                if (emailRegex.IsMatch(email))
                {
                    validEmails.Add(email);
                }
            }

            if (!validEmails.Any())
            {
                throw new ArgumentException("Không có email nào hợp lệ trong file Excel.");
            }

            // Check existing whitelist emails in database
            var existingEmails = (await _uow.AllowedEmails.GetAllAsync())
                .Select(ae => ae.Email)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newEmails = validEmails.Where(email => !existingEmails.Contains(email)).ToList();

            if (newEmails.Any())
            {
                var allowedEmailsToInsert = newEmails.Select(email => new AllowedEmail
                {
                    Email = email,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _uow.AllowedEmails.AddRangeAsync(allowedEmailsToInsert);
                await _uow.SaveChangesAsync();
                await _notificationService.SendNotificationAsync("AllowedEmailChanged", new { action = "Import", count = newEmails.Count });
            }

            _logger.LogInformation("Imported {Count} new whitelisted emails.", newEmails.Count);
            return newEmails.Count;
        }

        public async Task<IEnumerable<AllowedEmailDto>> GetAllowedEmailsAsync()
        {
            var allowedEmails = await _uow.AllowedEmails.GetAllAsync();
            var ordered = allowedEmails.OrderByDescending(ae => ae.CreatedAt);
            return _mapper.Map<IEnumerable<AllowedEmailDto>>(ordered);
        }

        public async Task DeleteAllowedEmailAsync(int id)
        {
            var allowedEmail = await _uow.AllowedEmails.GetByIdAsync(id);
            if (allowedEmail != null)
            {
                _uow.AllowedEmails.Remove(allowedEmail);
                await _uow.SaveChangesAsync();
                await _notificationService.SendNotificationAsync("AllowedEmailChanged", new { action = "Delete", id = id });
            }
        }

        public async Task CreateTeacherAccountAsync(string email, string fullName, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Email, họ tên và mật khẩu không được để trống.");
            }

            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
            {
                throw new ArgumentException("Định dạng email không hợp lệ.");
            }

            if (password.Length < 6)
            {
                throw new ArgumentException("Mật khẩu phải chứa ít nhất 6 ký tự.");
            }

            if (await _uow.Users.IsEmailExistsAsync(email))
            {
                throw new InvalidOperationException("Email này đã tồn tại trong hệ thống.");
            }

            if (await _uow.Users.IsUsernameExistsAsync(email))
            {
                throw new InvalidOperationException("Tên đăng nhập này đã tồn tại trong hệ thống.");
            }

            var role = await _uow.Roles.GetByNameAsync(AppConstants.RoleTeacher);
            if (role == null)
            {
                throw new InvalidOperationException("Không tìm thấy vai trò Giảng viên (Teacher) trong hệ thống.");
            }

            var newUser = new User
            {
                Username = email,
                Email = email,
                FullName = fullName,
                PasswordHash = PasswordHelper.HashPassword(password),
                IsActive = true,
                CanUpload = true,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Users.AddAsync(newUser);
            await _uow.SaveChangesAsync();

            await _uow.UserRoles.AssignRoleAsync(newUser.UserID, role.RoleID);
            await _uow.SaveChangesAsync();
            await _notificationService.SendNotificationAsync("UserChanged", new { action = "CreateTeacher", user = _mapper.Map<UserDto>(newUser) });
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng.");
            }

            var roles = await _uow.UserRoles.FindAsync(ur => ur.UserID == userId);
            var isAdmin = false;
            foreach (var ur in roles)
            {
                var role = await _uow.Roles.GetByIdAsync(ur.RoleID);
                if (role?.RoleName == AppConstants.RoleAdmin)
                {
                    isAdmin = true;
                    break;
                }
            }

            if (isAdmin)
            {
                throw new InvalidOperationException("Không thể xóa tài khoản Quản trị viên (Admin).");
            }

            _uow.Users.Remove(user);
            await _uow.SaveChangesAsync();
            await _notificationService.SendNotificationAsync("UserChanged", new { action = "Delete", userId = userId });
        }

        public async Task UpdateUploadPermissionAsync(int userId, bool canUpload)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng.");
            }

            var roles = await _uow.UserRoles.FindAsync(ur => ur.UserID == userId);
            var isTeacher = false;
            foreach (var ur in roles)
            {
                var role = await _uow.Roles.GetByIdAsync(ur.RoleID);
                if (role?.RoleName == AppConstants.RoleTeacher)
                {
                    isTeacher = true;
                    break;
                }
            }

            if (!isTeacher)
            {
                throw new InvalidOperationException("Chỉ giảng viên mới có quyền upload.");
            }

            user.CanUpload = canUpload;
            await _uow.SaveChangesAsync();
            await _notificationService.SendNotificationAsync("UserChanged", new { action = "UpdateUploadPermission", userId = userId, canUpload = canUpload });
        }
    }
}
