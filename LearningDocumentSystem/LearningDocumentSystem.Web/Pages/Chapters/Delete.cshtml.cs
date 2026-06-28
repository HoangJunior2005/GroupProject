using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using LearningDocumentSystem.Common.Constants;
using System.Linq;

namespace LearningDocumentSystem.Web.Pages.Chapters
{
    [Authorize(Policy = "TeacherUp")]
    public class DeleteModel : PageModel
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;

        public DeleteModel(IChapterService chapterService, ISubjectService subjectService)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var c = await _chapterService.GetByIdAsync(id);
            if (c == null) return NotFound();

            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var subjects = await _subjectService.GetAllAsync();
                    var isLeader = subjects.Any(s => s.SubjectID == c.SubjectID && s.SubjectLeaderID == userId);
                    if (!isLeader)
                    {
                        TempData["Error"] = "Bạn không có quyền xóa chương học của môn học này.";
                        return RedirectToPage("./Index");
                    }
                }
            }

            int? subjectId = c?.SubjectID;
            
            try
            {
                await _chapterService.DeleteAsync(id);
                TempData["Success"] = "Xóa chương học thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            
            return RedirectToPage("./Index", new { subjectId = subjectId });
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("./Index");
        }
    }
}
