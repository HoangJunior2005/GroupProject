using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LearningDocumentSystem.Web.Pages.Packages
{
    [AllowAnonymous]
    public class IpnModel : PageModel
    {
        private readonly IVnpayService _vnpayService;
        private readonly IPackageService _packageService;

        public IpnModel(IVnpayService vnpayService, IPackageService packageService)
        {
            _vnpayService = vnpayService;
            _packageService = packageService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = _vnpayService.ValidatePayment(Request.Query);
            if (!result.IsValid)
                return new JsonResult(new { RspCode = "97", Message = "Invalid signature" });
            if (!result.IsSuccess)
                return new JsonResult(new { RspCode = "01", Message = "Transaction failed" });

            await _packageService.SetPlanAsync(result.UserId, result.PlanCode);
            return new JsonResult(new { RspCode = "00", Message = "Confirm Success" });
        }
    }
}
