using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class PackageService : IPackageService
    {
        public const string FreeCode = "Free";
        public const string PlusCode = "Plus";
        public const string ProCode = "Pro";

        private static readonly HashSet<string> SupportedCodes =
            new(new[] { FreeCode, PlusCode, ProCode }, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> SupportedProviders =
            new(new[] { "Gemini", "Groq", "OpenAI" }, StringComparer.OrdinalIgnoreCase);
        private static readonly SemaphoreSlim CatalogLock = new(1, 1);
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly IUnitOfWork _uow;
        private readonly string _catalogPath;

        public PackageService(IUnitOfWork uow, IHostEnvironment environment)
        {
            _uow = uow;
            _catalogPath = Path.Combine(environment.ContentRootPath, "App_Data", "package-plans.json");
        }

        public async Task<IReadOnlyList<PackagePlanDto>> GetPlansAsync(int userId)
        {
            var plans = await LoadPlansAsync();
            var current = await GetCurrentPlanCodeAsync(userId, plans);
            return plans.Select(plan => CopyPlan(plan, plan.Code == current)).ToList();
        }

        public async Task<PackageStatusDto> GetStatusAsync(int userId)
        {
            var plans = await LoadPlansAsync();
            var currentCode = await GetCurrentPlanCodeAsync(userId, plans);
            var plan = plans.FirstOrDefault(x => x.Code == currentCode && x.IsActive)
                ?? plans.First(x => x.Code == FreeCode);
            var used = await _uow.ChatSessions.CountUserQuestionsSinceAsync(userId, GetVietnamDayStartUtc());

            return new PackageStatusDto
            {
                CurrentPlan = plan.Code,
                UsedToday = used,
                DailyMessageLimit = plan.DailyMessageLimit,
                RemainingToday = plan.DailyMessageLimit.HasValue
                    ? Math.Max(0, plan.DailyMessageLimit.Value - used)
                    : null,
                AllowedProviders = new List<string>(plan.AllowedProviders)
            };
        }

        public async Task<ChatAccessDto> ValidateChatAccessAsync(int userId, string? provider)
        {
            var status = await GetStatusAsync(userId);
            var selectedProvider = string.IsNullOrWhiteSpace(provider) ? "Gemini" : provider.Trim();

            if (!status.AllowedProviders.Contains(selectedProvider, StringComparer.OrdinalIgnoreCase))
            {
                return new ChatAccessDto
                {
                    IsAllowed = false,
                    Status = status,
                    Message = $"G\u00f3i {status.CurrentPlan} kh\u00f4ng h\u1ed7 tr\u1ee3 m\u00f4 h\u00ecnh {selectedProvider}. Vui l\u00f2ng n\u00e2ng c\u1ea5p g\u00f3i \u0111\u1ec3 ti\u1ebfp t\u1ee5c."
                };
            }

            if (status.DailyMessageLimit.HasValue && status.UsedToday >= status.DailyMessageLimit.Value)
            {
                return new ChatAccessDto
                {
                    IsAllowed = false,
                    Status = status,
                    Message = $"B\u1ea1n \u0111\u00e3 d\u00f9ng h\u1ebft {status.DailyMessageLimit.Value} c\u00e2u h\u1ecfi c\u1ee7a g\u00f3i {status.CurrentPlan} h\u00f4m nay."
                };
            }

            return new ChatAccessDto { IsAllowed = true, Status = status };
        }

        public async Task SetPlanAsync(int userId, string planCode)
        {
            var plans = await LoadPlansAsync();
            var normalizedPlan = plans.FirstOrDefault(x =>
                x.Code.Equals(planCode, StringComparison.OrdinalIgnoreCase) && x.IsActive)?.Code
                ?? throw new ArgumentException("Goi dich vu khong hop le hoac dang tam dung.", nameof(planCode));

            var user = await _uow.Users.GetWithRolesAsync(userId)
                ?? throw new InvalidOperationException("Khong tim thay nguoi dung.");
            if (!user.UserRoles.Any(x => x.Role.RoleName == "Student"))
                throw new InvalidOperationException("Chi tai khoan sinh vien moi co the su dung goi dich vu.");

            foreach (var paidRoleName in new[] { PlusCode, ProCode })
            {
                var paidRole = await _uow.Roles.GetByNameAsync(paidRoleName);
                if (paidRole != null)
                    await _uow.UserRoles.RemoveRoleAsync(userId, paidRole.RoleID);
            }

            if (normalizedPlan != FreeCode)
            {
                var role = await _uow.Roles.GetByNameAsync(normalizedPlan)
                    ?? throw new InvalidOperationException($"Role goi {normalizedPlan} chua duoc khoi tao.");
                await _uow.UserRoles.AssignRoleAsync(userId, role.RoleID);
            }

            await _uow.SaveChangesAsync();
        }

        public PackagePlanDto? FindPlan(string planCode)
        {
            var plan = LoadPlans().FirstOrDefault(x =>
                x.Code.Equals(planCode, StringComparison.OrdinalIgnoreCase));
            return plan == null ? null : CopyPlan(plan, false);
        }

        public async Task CreatePlanAsync(PackagePlanDto plan)
        {
            ValidatePlan(plan);
            await CatalogLock.WaitAsync();
            try
            {
                var plans = LoadPlans();
                if (plans.Any(x => x.Code.Equals(plan.Code, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Ma goi da ton tai.");
                plans.Add(CopyPlan(plan, false));
                await SavePlansAsync(plans);
            }
            finally { CatalogLock.Release(); }
        }

        public async Task UpdatePlanAsync(PackagePlanDto plan)
        {
            ValidatePlan(plan);
            await CatalogLock.WaitAsync();
            try
            {
                var plans = LoadPlans();
                var index = plans.FindIndex(x => x.Code.Equals(plan.Code, StringComparison.OrdinalIgnoreCase));
                if (index < 0) throw new InvalidOperationException("Khong tim thay goi can cap nhat.");
                plans[index] = CopyPlan(plan, false);
                await SavePlansAsync(plans);
            }
            finally { CatalogLock.Release(); }
        }

        public async Task DeletePlanAsync(string planCode)
        {
            if (planCode.Equals(FreeCode, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Khong the xoa goi Free mac dinh.");

            await CatalogLock.WaitAsync();
            try
            {
                var plans = LoadPlans();
                var removed = plans.RemoveAll(x => x.Code.Equals(planCode, StringComparison.OrdinalIgnoreCase));
                if (removed == 0) throw new InvalidOperationException("Khong tim thay goi can xoa.");
                await SavePlansAsync(plans);
            }
            finally { CatalogLock.Release(); }
        }

        private async Task<string> GetCurrentPlanCodeAsync(int userId, IReadOnlyList<PackagePlanDto> plans)
        {
            var roles = (await _uow.Users.GetUserRolesAsync(userId)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (roles.Contains(ProCode) && plans.Any(x => x.Code == ProCode && x.IsActive)) return ProCode;
            if (roles.Contains(PlusCode) && plans.Any(x => x.Code == PlusCode && x.IsActive)) return PlusCode;
            return FreeCode;
        }

        private List<PackagePlanDto> LoadPlans()
        {
            if (!File.Exists(_catalogPath))
                throw new InvalidOperationException("Khong tim thay file cau hinh package-plans.json.");
            var plans = JsonSerializer.Deserialize<List<PackagePlanDto>>(File.ReadAllText(_catalogPath), JsonOptions)
                ?? new List<PackagePlanDto>();
            if (!plans.Any(x => x.Code == FreeCode))
                throw new InvalidOperationException("Catalog bat buoc phai co goi Free.");
            return plans;
        }

        private Task<List<PackagePlanDto>> LoadPlansAsync() => Task.FromResult(LoadPlans());

        private async Task SavePlansAsync(List<PackagePlanDto> plans)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_catalogPath)!);
            var tempPath = _catalogPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(plans.OrderBy(PlanOrder), JsonOptions));
            File.Move(tempPath, _catalogPath, true);
        }

        private static int PlanOrder(PackagePlanDto plan) => plan.Code switch
        {
            FreeCode => 0,
            PlusCode => 1,
            ProCode => 2,
            _ => 3
        };

        private static void ValidatePlan(PackagePlanDto plan)
        {
            plan.Code = SupportedCodes.FirstOrDefault(x => x.Equals(plan.Code?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Chi ho tro ba ma goi Free, Plus va Pro.");
            plan.Name = string.IsNullOrWhiteSpace(plan.Name) ? plan.Code : plan.Name.Trim();
            if (plan.Price < 0) throw new InvalidOperationException("Gia goi khong the am.");
            if (plan.Code == FreeCode)
            {
                plan.Price = 0;
                plan.IsActive = true;
            }
            if (plan.DailyMessageLimit <= 0) plan.DailyMessageLimit = null;
            plan.AllowedProviders = plan.AllowedProviders
                .Where(SupportedProviders.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (plan.AllowedProviders.Count == 0)
                throw new InvalidOperationException("Goi phai cho phep it nhat mot mo hinh AI.");
            plan.Features = plan.Features.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        }

        private static DateTime GetVietnamDayStartUtc()
        {
            TimeZoneInfo timeZone;
            try { timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch { timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(now.Date, DateTimeKind.Unspecified), timeZone);
        }

        private static PackagePlanDto CopyPlan(PackagePlanDto plan, bool isCurrent) => new()
        {
            Code = plan.Code,
            Name = plan.Name,
            Price = plan.Price,
            DailyMessageLimit = plan.DailyMessageLimit,
            AllowedProviders = new List<string>(plan.AllowedProviders),
            Features = new List<string>(plan.Features),
            IsCurrent = isCurrent,
            IsActive = plan.IsActive
        };
    }
}
