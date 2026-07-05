using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace LearningDocumentSystem.Web.Pages.Benchmarks
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;

        public BenchmarkStatsDto Stats { get; set; } = new();
        public string StatsJson { get; set; } = "{}";

        public IndexModel(IBenchmarkService benchmarkService)
        {
            _benchmarkService = benchmarkService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Stats = await _benchmarkService.GetDashboardStatsAsync();
            StatsJson = JsonSerializer.Serialize(Stats, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return Page();
        }
    }
}
