using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Admin
{
    [Authorize(Roles = AppConstants.RoleAdmin)]
    public class RevenueModel : PageModel
    {
        private readonly IPackageService _packageService;

        public RevenueModel(IPackageService packageService)
        {
            _packageService = packageService;
        }

        public RevenueDashboardDto Dashboard { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedMonth { get; set; }

        public List<int> AvailableYears { get; set; } = new();

        public async Task OnGetAsync()
        {
            var now = DateTime.UtcNow;
            
            // Chuyển múi giờ Việt Nam nếu cần thiết
            TimeZoneInfo tz;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);

            SelectedYear ??= localNow.Year;
            SelectedMonth ??= localNow.Month;

            Dashboard = await _packageService.GetRevenueStatsAsync(SelectedYear, SelectedMonth);

            // Các năm hiển thị lọc (từ 2024 đến năm hiện tại + 1)
            for (int y = 2024; y <= localNow.Year + 1; y++)
            {
                AvailableYears.Add(y);
            }
        }
    }
}
