using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Web.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class BenchmarksModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;
        private readonly ILogger<BenchmarksModel> _logger;

        public BenchmarksModel(
            IBenchmarkService benchmarkService,
            ILogger<BenchmarksModel> logger)
        {
            _benchmarkService = benchmarkService;
            _logger = logger;
        }

        public BenchmarkDashboardDto Dashboard { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Dashboard = await _benchmarkService.GetDashboardDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics dashboard.");
                return RedirectToPage("/Error");
            }
        }

        public async Task<IActionResult> OnGetDashboardDataAsync()
        {
            try
            {
                var data = await _benchmarkService.GetDashboardDataAsync();
                return new JsonResult(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data via AJAX.");
                return new JsonResult(new { success = false });
            }
        }
    }
}
