using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LearningDocumentSystem.Web.Pages.Admin
{
    [Authorize(Roles = AppConstants.RoleAdmin + "," + AppConstants.RoleTeacher)]
    public class BenchmarksModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;

        public BenchmarksModel(
            IBenchmarkService benchmarkService,
            ISubjectService subjectService,
            IChapterService chapterService)
        {
            _benchmarkService = benchmarkService;
            _subjectService = subjectService;
            _chapterService = chapterService;
        }

        public BenchmarkDashboardDto Dashboard { get; private set; } = new();
        public IReadOnlyList<SubjectDto> Subjects { get; private set; } = Array.Empty<SubjectDto>();
        public IReadOnlyList<ChapterDto> Chapters { get; private set; } = Array.Empty<ChapterDto>();

        public async Task OnGetAsync()
        {
            Dashboard = await _benchmarkService.GetDashboardAsync();
            Subjects = (await _subjectService.GetAllAsync()).OrderBy(x => x.SubjectName).ToList();
            Chapters = (await _chapterService.GetAllAsync())
                .OrderBy(x => x.SubjectName).ThenBy(x => x.ChapterNumber).ToList();
        }

        public async Task<IActionResult> OnPostRunRetrievalAsync(
            [FromForm] string question,
            [FromForm] int? subjectId,
            [FromForm] int? chapterId)
        {
            if (string.IsNullOrWhiteSpace(question))
                return BadRequest(new { message = "Vui lòng nhập câu hỏi thử nghiệm." });

            var result = await _benchmarkService.RunRetrievalAsync(question, subjectId, chapterId);
            return new JsonResult(result);
        }
    }
}
