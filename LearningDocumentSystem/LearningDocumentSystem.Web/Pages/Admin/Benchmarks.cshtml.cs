using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LearningDocumentSystem.Web.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class BenchmarksModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly ILogger<BenchmarksModel> _logger;

        public BenchmarksModel(
            IBenchmarkService benchmarkService,
            ISubjectService subjectService,
            IChapterService chapterService,
            ILogger<BenchmarksModel> logger)
        {
            _benchmarkService = benchmarkService;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _logger = logger;
        }

        public BenchmarkDashboardDto Dashboard { get; set; } = new();
        public List<SelectListItem> Subjects { get; set; } = new();
        public List<SelectListItem> Chapters { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Dashboard = await _benchmarkService.GetDashboardDataAsync();
                await LoadFiltersAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading benchmark dashboard.");
                return RedirectToPage("/Error");
            }
        }

        /// <summary>
        /// AJAX handler: Chạy thử nghiệm Playground
        /// </summary>
        public async Task<IActionResult> OnPostRunPlaygroundAsync(
            [FromBody] PlaygroundRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                return new JsonResult(new { success = false, message = "Vui lòng nhập câu hỏi." });
            }

            try
            {
                var results = await _benchmarkService.RunPlaygroundAsync(
                    request.Question, request.SubjectId, request.ChapterId);

                return new JsonResult(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running playground benchmark.");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi chạy thử nghiệm." });
            }
        }

        /// <summary>
        /// AJAX handler: Lấy chapters theo subject
        /// </summary>
        public async Task<IActionResult> OnGetChaptersBySubjectAsync(int subjectId)
        {
            try
            {
                var chapters = await _chapterService.GetBySubjectAsync(subjectId);
                var items = chapters.Select(c => new
                {
                    value = c.ChapterID,
                    text = $"Chương {c.ChapterNumber}: {c.ChapterName}"
                });
                return new JsonResult(items);
            }
            catch
            {
                return new JsonResult(Array.Empty<object>());
            }
        }

        private async Task LoadFiltersAsync()
        {
            var subjects = await _subjectService.GetAllAsync();
            Subjects = subjects.Select(s => new SelectListItem
            {
                Value = s.SubjectID.ToString(),
                Text = $"{s.SubjectCode} — {s.SubjectName}"
            }).ToList();
        }

        public class PlaygroundRequest
        {
            public string Question { get; set; } = string.Empty;
            public int? SubjectId { get; set; }
            public int? ChapterId { get; set; }
        }
    }
}
