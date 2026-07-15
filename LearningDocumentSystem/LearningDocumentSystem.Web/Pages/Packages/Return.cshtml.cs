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

        /// <summary>Ngày hết hạn gói (UtcNow + 1 tháng), chỉ có giá trị khi thanh toán thành công.</summary>
        public DateTime? PlanExpiresAt { get; private set; }

        public async Task OnGetAsync()
        {
            Result = _vnpayService.ValidatePayment(Request.Query);
            if (Result.IsValid)
            {
                await _packageService.RecordTransactionAsync(Result.UserId, Result.PlanCode, Result.Amount, Result.TransactionReference, Result.IsSuccess);

                if (Result.IsSuccess)
                {
                    await _packageService.SetPlanAsync(Result.UserId, Result.PlanCode);
                    // Tính ngày hết hạn: 1 tháng từ lúc kích hoạt
                    if (Result.Amount > 0)
                        PlanExpiresAt = DateTime.UtcNow.AddMonths(1);
                }
            }
        }
    }
}
