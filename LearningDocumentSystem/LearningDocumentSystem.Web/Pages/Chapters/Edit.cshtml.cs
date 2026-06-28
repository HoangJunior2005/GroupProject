using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using LearningDocumentSystem.Common.Constants;
using System.Linq;
using System.Collections.Generic;

namespace LearningDocumentSystem.Web.Pages.Chapters
{
    [Authorize(Policy = "TeacherUp")]
    public class EditModel : PageModel
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;

        public EditModel(IChapterService chapterService, ISubjectService subjectService)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
        }

        [BindProperty]
        public ChapterFormViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var c = await _chapterService.GetByIdAsync(id);
            if (c == null) return NotFound();

            var allowed = await GetAllowedSubjectsAsync();
            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                if (!allowed.Any(s => s.SubjectID == c.SubjectID))
                {
                    TempData["Error"] = "Bạn không có quyền sửa chương học của môn học này.";
                    return RedirectToPage("./Index");
                }
            }

            Input = new ChapterFormViewModel
            {
                ChapterID = c.ChapterID,
                SubjectID = c.SubjectID,
                ChapterNumber = c.ChapterNumber,
                ChapterName = c.ChapterName,
                Subjects = allowed
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var allowed = await GetAllowedSubjectsAsync();
            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                if (!allowed.Any(s => s.SubjectID == Input.SubjectID))
                {
                    ModelState.AddModelError(string.Empty, "Bạn không có quyền di chuyển hoặc gán chương học vào môn học này.");
                    Input.Subjects = allowed;
                    return Page();
                }

                var originalChapter = await _chapterService.GetByIdAsync(Input.ChapterID);
                if (originalChapter == null || !allowed.Any(s => s.SubjectID == originalChapter.SubjectID))
                {
                    ModelState.AddModelError(string.Empty, "Bạn không có quyền chỉnh sửa chương học này.");
                    Input.Subjects = allowed;
                    return Page();
                }
            }

            if (!ModelState.IsValid)
            {
                Input.Subjects = allowed;
                return Page();
            }

            try
            {
                await _chapterService.UpdateAsync(new UpdateChapterDto
                {
                    ChapterID = Input.ChapterID,
                    SubjectID = Input.SubjectID,
                    ChapterNumber = Input.ChapterNumber,
                    ChapterName = Input.ChapterName
                });
                TempData["Success"] = "Cập nhật chương học thành công.";
                return RedirectToPage("./Index", new { subjectId = Input.SubjectID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                Input.Subjects = allowed;
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

            return all;
        }
    }
}
