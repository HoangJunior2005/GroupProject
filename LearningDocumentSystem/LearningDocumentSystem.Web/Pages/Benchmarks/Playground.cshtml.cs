using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace LearningDocumentSystem.Web.Pages.Benchmarks
{
    [Authorize(Policy = "AdminOnly")]
    public class PlaygroundModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;

        public List<SubjectDto> Subjects { get; set; } = new();
        public List<ChapterDto> Chapters { get; set; } = new();

        public PlaygroundModel(
            IBenchmarkService benchmarkService,
            ISubjectService subjectService,
            IChapterService chapterService)
        {
            _benchmarkService = benchmarkService;
            _subjectService = subjectService;
            _chapterService = chapterService;
        }

        public async Task OnGetAsync()
        {
            Subjects = (await _subjectService.GetAllAsync()).ToList();
            Chapters = (await _chapterService.GetAllAsync()).ToList();
        }

        /// <summary>AJAX endpoint: nhận PlaygroundRequest, trả về kết quả JSON.</summary>
        public async Task<IActionResult> OnPostRunAsync([FromBody] PlaygroundRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.QueryText))
                return BadRequest(new { error = "Query text is required." });

            var result = await _benchmarkService.RunPlaygroundAsync(request);
            return new JsonResult(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
