using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace LearningDocumentSystem.Web.Pages.Packages
{
    [Authorize(Roles = AppConstants.RoleStudent)]
    public class IndexModel : PageModel
    {
        private readonly IPackageService _packageService;
        private readonly IVnpayService _vnpayService;

        public IndexModel(IPackageService packageService, IVnpayService vnpayService)
        {
            _packageService = packageService;
            _vnpayService = vnpayService;
        }

        public IReadOnlyList<PackagePlanDto> Plans { get; private set; } = Array.Empty<PackagePlanDto>();
        public PackageStatusDto Status { get; private set; } = new();
        public bool VnpayConfigured => _vnpayService.IsConfigured;

        public async Task OnGetAsync()
        {
            var userId = GetCurrentUserId();
            Plans = (await _packageService.GetPlansAsync(userId)).Where(x => x.IsActive).ToList();
            Status = await _packageService.GetStatusAsync(userId);
        }

        public async Task<IActionResult> OnPostCheckoutAsync(string planCode)
        {
            var userId = GetCurrentUserId();
            var plan = _packageService.FindPlan(planCode);
            if (plan == null || !plan.IsActive)
            {
                TempData["Error"] = "Goi dich vu khong hop le.";
                return RedirectToPage();
            }

            if (plan.Price == 0)
            {
                var now = DateTime.UtcNow;
                var txnRef = $"LDS{userId}Free{now:yyyyMMddHHmmssfff}";
                await _packageService.RecordTransactionAsync(userId, plan.Code, 0, txnRef, true);
                await _packageService.SetPlanAsync(userId, plan.Code);
                TempData["Success"] = "Da chuyen ve goi Free.";
                return RedirectToPage();
            }

            try
            {
                var returnUrl = Url.Page("/Packages/Return", null, null, Request.Scheme)
                    ?? throw new InvalidOperationException("Khong tao duoc Return URL.");
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var paymentUrl = _vnpayService.CreatePaymentUrl(plan, userId, ipAddress, returnUrl);
                return Redirect(paymentUrl);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage();
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("UserID") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }
}
