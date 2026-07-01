using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace LearningDocumentSystem.Web.Pages.Documents
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IConfiguration _config;

        public IndexModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IConfiguration config)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _config = config;
        }

        public IEnumerable<DocumentDto> Documents { get; set; } = [];
        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
        public IEnumerable<ChapterDto> Chapters { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ChapterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageSize => _config.GetValue<int>("AppSettings:DefaultPageSize", 10);

        public async Task OnGetAsync()
        {
            if (!SubjectId.HasValue && ChapterId.HasValue)
            {
                var selectedChapter = await _chapterService.GetByIdAsync(ChapterId.Value);
                if (selectedChapter != null)
                {
                    SubjectId = selectedChapter.SubjectID;
                }
            }

            int? teacherId = null;
            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    teacherId = userId;
                }
            }

            var (items, total) = await _documentService.GetPagedAsync(
                Keyword, SubjectId, ChapterId, Status, teacherId, CurrentPage, PageSize);

            Documents = items;
            TotalCount = total;
            TotalPages = (int)Math.Ceiling(total / (double)PageSize);

            Subjects = await _subjectService.GetAllAsync();
            if (teacherId.HasValue)
            {
                Subjects = Subjects.Where(s => s.SubjectLeaderID == teacherId.Value).ToList();
            }

            Chapters = SubjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(SubjectId.Value)
                : [];
        }

        // AJAX handler to load chapters for a subject dropdown
        public async Task<IActionResult> OnGetGetChaptersAsync(int subjectId)
        {
            var chapters = await _chapterService.GetBySubjectAsync(subjectId);
            return new JsonResult(chapters.Select(c => new { c.ChapterID, c.ChapterName, c.ChapterNumber }));
        }

        // AJAX handler to load all subjects (filtered by teacher if applicable)
        public async Task<IActionResult> OnGetGetSubjectsAsync()
        {
            var subjects = await _subjectService.GetAllAsync();

            // If the current user is a Teacher, only return subjects they are leading
            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    subjects = subjects.Where(s => s.SubjectLeaderID == userId).ToList();
                }
            }

            return new JsonResult(subjects.Select(s => new { s.SubjectID, s.SubjectName, s.SubjectCode }));
        }
    }
}
