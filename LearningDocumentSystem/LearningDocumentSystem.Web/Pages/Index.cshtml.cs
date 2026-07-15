using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IPackageService _packageService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IDocumentService documentService, IPackageService packageService, ILogger<IndexModel> logger)
        {
            _documentService = documentService;
            _packageService = packageService;
            _logger = logger;
        }

        public DashboardDto Dashboard { get; private set; } = new();
        public RevenueDashboardDto RevenueDashboard { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedMonth { get; set; }

        public List<int> AvailableYears { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Teacher"))
            {
                return RedirectToPage("/Documents/Index");
            }

            try
            {
                Dashboard = await _documentService.GetDashboardAsync();

                if (User.IsInRole("Admin"))
                {
                    var now = DateTime.UtcNow;
                    TimeZoneInfo tz;
                    try { tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
                    catch { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
                    var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);

                    SelectedYear ??= localNow.Year;
                    SelectedMonth ??= localNow.Month;

                    RevenueDashboard = await _packageService.GetRevenueStatsAsync(SelectedYear, SelectedMonth);

                    for (int y = 2024; y <= localNow.Year + 1; y++)
                    {
                        AvailableYears.Add(y);
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard.");
                return RedirectToPage("/Error");
            }
        }
    }
}
