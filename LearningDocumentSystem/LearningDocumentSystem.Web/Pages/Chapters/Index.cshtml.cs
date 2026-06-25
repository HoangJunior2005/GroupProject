using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using LearningDocumentSystem.Common.Constants;

namespace LearningDocumentSystem.Web.Pages.Chapters
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;

        public IndexModel(IChapterService chapterService, ISubjectService subjectService)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
        }

        public IEnumerable<ChapterDto> Chapters { get; set; } = [];
        public IEnumerable<SubjectDto> Subjects { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public int? SubjectId { get; set; }

        public async Task OnGetAsync()
        {
            Subjects = await _subjectService.GetAllAsync();
            var allChapters = await _chapterService.GetAllAsync();

            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    Subjects = Subjects.Where(s => s.SubjectLeaderID == userId).ToList();
                    allChapters = allChapters.Where(c => Subjects.Any(s => s.SubjectID == c.SubjectID)).ToList();
                }
            }

            if (SubjectId.HasValue)
            {
                // Ensure the selected subject is in the allowed list for this user
                if (Subjects.Any(s => s.SubjectID == SubjectId.Value))
                {
                    Chapters = allChapters.Where(c => c.SubjectID == SubjectId.Value).ToList();
                }
                else
                {
                    Chapters = new List<ChapterDto>(); // Forbidden or no match
                }
            }
            else
            {
                Chapters = allChapters;
            }
        }
    }
}
