using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Data.Seeders
{
    public class DataSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Apply pending migrations
                await _context.Database.MigrateAsync();

                await SeedRolesAsync();
                await SeedUsersAsync();
                await SeedSubjectsAsync();
                await SeedDocumentsAsync();
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Database seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seeding database.");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            if (await _context.Roles.AnyAsync()) return;

            _logger.LogInformation("Seeding roles...");
            var roles = new List<Role>
            {
                new() { RoleName = "Admin" },
                new() { RoleName = "Teacher" },
                new() { RoleName = "Student" }
            };
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync(); // Save để lấy RoleID
        }

        private async Task SeedUsersAsync()
        {
            _logger.LogInformation("Seeding users and roles...");

            var adminRole   = await _context.Roles.FirstAsync(r => r.RoleName == "Admin");
            var teacherRole = await _context.Roles.FirstAsync(r => r.RoleName == "Teacher");
            var studentRole = await _context.Roles.FirstAsync(r => r.RoleName == "Student");

            var now = DateTime.UtcNow;
            var random = new Random(42);

            // 1. Core Users
            var coreUsers = new List<(User User, string RoleName)>
            {
                (new User
                {
                    Username     = "admin",
                    PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                    FullName     = "Quản Trị Viên",
                    Email        = "admin@university.edu.vn",
                    IsActive     = true,
                    CanUpload    = true,
                    CreatedAt    = now
                }, "Admin"),
                (new User
                {
                    Username     = "nguyenvan_gv",
                    PasswordHash = PasswordHelper.HashPassword("Teacher@123"),
                    FullName     = "Nguyễn Văn Giảng Viên",
                    Email        = "teacher@university.edu.vn",
                    IsActive     = true,
                    CanUpload    = true,
                    CreatedAt    = now
                }, "Teacher"),
                (new User
                {
                    Username     = "tranmanh_sv",
                    PasswordHash = PasswordHelper.HashPassword("Student@123"),
                    FullName     = "Trần Mạnh Sinh Viên",
                    Email        = "student@student.edu.vn",
                    IsActive     = true,
                    CreatedAt    = now
                }, "Student")
            };

            // 2. Historical Users
            var firstNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Phan", "Vũ", "Võ", "Đặng", "Bùi" };
            var middleNames = new[] { "Văn", "Thị", "Hữu", "Minh", "Quốc", "Đức", "Gia", "Thanh", "Ngọc", "Anh" };
            var lastNames = new[] { "Anh", "Bình", "Chương", "Duy", "Giang", "Hải", "Khánh", "Linh", "Nam", "Sơn" };

            var allSeedUsers = new List<(User User, string RoleName)>();
            allSeedUsers.AddRange(coreUsers);

            for (int i = 1; i <= 11; i++)
            {
                var createdDate = new DateTime(now.Year, now.Month, 1).AddMonths(-i).AddDays(random.Next(1, 28));

                var teacher = new User
                {
                    Username     = $"gv_thang{now.AddMonths(-i).Month}",
                    PasswordHash = PasswordHelper.HashPassword("Teacher@123"),
                    FullName     = $"{firstNames[random.Next(firstNames.Length)]} {middleNames[random.Next(middleNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                    Email        = $"gv_thang{now.AddMonths(-i).Month}@university.edu.vn",
                    IsActive     = true,
                    CanUpload    = true,
                    CreatedAt    = createdDate
                };
                allSeedUsers.Add((teacher, "Teacher"));

                int studentCount = random.Next(2, 5); 
                for (int j = 1; j <= studentCount; j++)
                {
                    var student = new User
                    {
                        Username     = $"sv_thang{now.AddMonths(-i).Month}_{j}",
                        PasswordHash = PasswordHelper.HashPassword("Student@123"),
                        FullName     = $"{firstNames[random.Next(firstNames.Length)]} {middleNames[random.Next(middleNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                        Email        = $"sv_thang{now.AddMonths(-i).Month}_{j}@student.edu.vn",
                        IsActive     = true,
                        CreatedAt    = createdDate
                    };
                    allSeedUsers.Add((student, "Student"));
                }
            }
            var existingUsernames = new HashSet<string>(
                await _context.Users.Select(u => u.Username).ToListAsync(),
                StringComparer.OrdinalIgnoreCase);

            var usersToAdd = new List<(User User, string RoleName)>();
            foreach (var item in allSeedUsers)
            {
                if (!existingUsernames.Contains(item.User.Username))
                {
                    usersToAdd.Add(item);
                }
            }

            if (usersToAdd.Any())
            {
                await _context.Users.AddRangeAsync(usersToAdd.Select(x => x.User));
                await _context.SaveChangesAsync(); // Saves all users and generates IDs

                var userRoles = new List<UserRole>();
                foreach (var item in usersToAdd)
                {
                    var role = item.RoleName switch
                    {
                        "Admin" => adminRole,
                        "Teacher" => teacherRole,
                        _ => studentRole
                    };

                    userRoles.Add(new UserRole
                    {
                        UserID = item.User.UserID,
                        RoleID = role.RoleID
                    });
                }
                await _context.UserRoles.AddRangeAsync(userRoles);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedSubjectsAsync()
        {
            if (await _context.Subjects.AnyAsync()) return;

            _logger.LogInformation("Seeding subjects and chapters...");

            // Môn học từ file Word
            var subject = new Subject
            {
                SubjectName = "Lập trình cấu trúc C#",
                SubjectCode = "INF205",
                CreatedAt   = DateTime.UtcNow
            };
            await _context.Subjects.AddAsync(subject);
            await _context.SaveChangesAsync();

            // Chương từ file Word
            var chapters = new List<Chapter>
            {
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 1,
                    ChapterName   = "Tổng quan về Biến cấu trúc trong .NET"
                },
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 2,
                    ChapterName   = "Kiểu dữ liệu, Toán tử và Biểu thức"
                },
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 3,
                    ChapterName   = "Cấu trúc điều kiện và vòng lặp"
                },
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 4,
                    ChapterName   = "Mảng, Chuỗi và Collection"
                }
            };
            await _context.Chapters.AddRangeAsync(chapters);
            await _context.SaveChangesAsync();
        }

        private async Task SeedDocumentsAsync()
        {
            if (await _context.Documents.AnyAsync(d => d.StoragePath.StartsWith("demo_"))) return;

            _logger.LogInformation("Seeding historical documents...");

            var teacher = await _context.Users.FirstOrDefaultAsync(u => u.Username == "nguyenvan_gv");
            var chapter = await _context.Chapters.FirstOrDefaultAsync();

            if (teacher == null || chapter == null)
            {
                _logger.LogWarning("Cannot seed documents because teacher or chapter is missing.");
                return;
            }

            var now = DateTime.UtcNow;
            var random = new Random(99);

            var docTitles = new[] 
            { 
                "Giáo trình C# cơ bản", "Bài tập thực hành OOP", "Slide bài giảng Chương 1", 
                "Tài liệu tham khảo .NET Core", "Hướng dẫn cài đặt Visual Studio", "Đề cương ôn tập",
                "Đề thi mẫu giữa kỳ", "Bài đọc thêm về Design Patterns", "Tổng quan về LINQ",
                "Xử lý ngoại lệ trong C#", "Làm việc với File và Stream", "Lập trình đa luồng cơ bản"
            };

            var fileTypes = new[] { "pdf", "docx", "pptx" };

            var documents = new List<Document>();

            for (int i = 1; i <= 11; i++)
            {
                var createdDate = new DateTime(now.Year, now.Month, 1).AddMonths(-i).AddDays(random.Next(1, 28));
                
                int docsCount = random.Next(1, 4); // 1 to 3 documents per month
                for (int j = 1; j <= docsCount; j++)
                {
                    var title = $"{docTitles[random.Next(docTitles.Length)]} - V{j} (Tháng {createdDate.Month})";
                    var ext = fileTypes[random.Next(fileTypes.Length)];
                    var uniqueId = Guid.NewGuid().ToString()[..8];
                    
                    var storagePath = $"demo_{uniqueId}.{ext}";
                    var document = new Document
                    {
                        ChapterID = chapter.ChapterID,
                        Title = title,
                        FileType = ext,
                        StoragePath = storagePath,
                        OriginalFileName = storagePath,
                        FileSizeInBytes = random.Next(102400, 5242880), // 100 KB to 5 MB
                        IndexStatus = "Indexed",
                        UploadedBy = teacher.UserID,
                        UploadedAt = createdDate,
                        FileHash = Guid.NewGuid().ToString().Replace("-", "")
                    };
                    documents.Add(document);
                }
            }

            await _context.Documents.AddRangeAsync(documents);
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Seeded {Count} historical documents successfully.", documents.Count);
        }
    }
}
