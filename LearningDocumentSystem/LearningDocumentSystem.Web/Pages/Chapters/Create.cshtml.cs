using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Chapters
{
    [Authorize(Policy = "TeacherUp")]
    public class CreateModel : PageModel
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;

        public CreateModel(IChapterService chapterService, ISubjectService subjectService)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
        }

        [BindProperty]
        public ChapterFormViewModel Input { get; set; } = new();

        public async Task OnGetAsync(int? subjectId)
        {
            Input.Subjects = await GetAllowedSubjectsAsync();
            Input.SubjectID = subjectId ?? 0;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Input.Subjects = await GetAllowedSubjectsAsync();
                return Page();
            }

            try
            {
                await _chapterService.CreateAsync(new CreateChapterDto
                {
                    SubjectID = Input.SubjectID,
                    ChapterNumber = Input.ChapterNumber,
                    ChapterName = Input.ChapterName
                });
                TempData["Success"] = "Tạo chương học thành công.";
                return RedirectToPage("./Index", new { subjectId = Input.SubjectID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                Input.Subjects = await GetAllowedSubjectsAsync();
                return Page();
            }
        }

        private async Task<IEnumerable<SubjectDto>> GetAllowedSubjectsAsync()
        {
            var all = await _subjectService.GetAllAsync();

            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    return all.Where(s => s.SubjectLeaderID == userId).ToList();
                }
            }

            // Admin: hiện tất cả
            return all;
        }
    }
}

