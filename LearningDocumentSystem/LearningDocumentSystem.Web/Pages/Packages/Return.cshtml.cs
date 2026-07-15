using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LearningDocumentSystem.Web.Pages.Packages
{
    [AllowAnonymous]
    public class ReturnModel : PageModel
    {
        private readonly IVnpayService _vnpayService;
        private readonly IPackageService _packageService;

        public ReturnModel(IVnpayService vnpayService, IPackageService packageService)
        {
            _vnpayService = vnpayService;
            _packageService = packageService;
        }

        public VnpayPaymentResultDto Result { get; private set; } = new();

        public async Task OnGetAsync()
        {
            Result = _vnpayService.ValidatePayment(Request.Query);
            if (Result.IsValid)
            {
                var plan = _packageService.FindPlan(Result.PlanCode);
                decimal amount = plan?.Price ?? 0;
                await _packageService.RecordTransactionAsync(Result.UserId, Result.PlanCode, amount, Result.TransactionReference, Result.IsSuccess);

                if (Result.IsSuccess)
                {
                    await _packageService.SetPlanAsync(Result.UserId, Result.PlanCode);
                }
            }
        }
    }
}
