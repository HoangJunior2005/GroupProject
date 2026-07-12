using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LearningDocumentSystem.Web.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class PackagesModel : PageModel
    {
        private readonly IPackageService _packageService;

        public PackagesModel(IPackageService packageService)
        {
            _packageService = packageService;
        }

        public IReadOnlyList<PackagePlanDto> Plans { get; private set; } = Array.Empty<PackagePlanDto>();
        public bool IsEditing { get; private set; }

        [BindProperty] public string Code { get; set; } = string.Empty;
        [BindProperty] public string Name { get; set; } = string.Empty;
        [BindProperty] public decimal Price { get; set; }
        [BindProperty] public int? DailyMessageLimit { get; set; }
        [BindProperty] public string[] Providers { get; set; } = Array.Empty<string>();
        [BindProperty] public string Features { get; set; } = string.Empty;
        [BindProperty] public bool IsActive { get; set; } = true;
        [BindProperty] public string? OriginalCode { get; set; }

        public async Task OnGetAsync(string? editCode)
        {
            await LoadPlansAsync();
            if (string.IsNullOrWhiteSpace(editCode)) return;

            var plan = Plans.FirstOrDefault(x => x.Code.Equals(editCode, StringComparison.OrdinalIgnoreCase));
            if (plan == null) return;

            IsEditing = true;
            OriginalCode = plan.Code;
            Code = plan.Code;
            Name = plan.Name;
            Price = plan.Price;
            DailyMessageLimit = plan.DailyMessageLimit;
            Providers = plan.AllowedProviders.ToArray();
            Features = string.Join(Environment.NewLine, plan.Features);
            IsActive = plan.IsActive;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var plan = new PackagePlanDto
            {
                Code = Code,
                Name = Name,
                Price = Price,
                DailyMessageLimit = DailyMessageLimit,
                AllowedProviders = Providers.ToList(),
                Features = Features.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                IsActive = IsActive
            };

            try
            {
                if (!string.IsNullOrWhiteSpace(OriginalCode)
                    && !OriginalCode.Equals(Code, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Khong the thay doi ma goi khi cap nhat.");

                if (string.IsNullOrWhiteSpace(OriginalCode))
                    await _packageService.CreatePlanAsync(plan);
                else
                    await _packageService.UpdatePlanAsync(plan);

                TempData["Success"] = string.IsNullOrWhiteSpace(OriginalCode)
                    ? "Da tao goi dich vu."
                    : "Da cap nhat goi dich vu.";
                return RedirectToPage();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                IsEditing = !string.IsNullOrWhiteSpace(OriginalCode);
                await LoadPlansAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string code)
        {
            try
            {
                await _packageService.DeletePlanAsync(code);
                TempData["Success"] = "Da xoa goi dich vu.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage();
        }

        private async Task LoadPlansAsync()
        {
            Plans = await _packageService.GetPlansAsync(0);
        }
    }
}
