using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Subjects
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IAdminUserService _adminUserService;

        public EditModel(ISubjectService subjectService, IAdminUserService adminUserService)
        {
            _subjectService = subjectService;
            _adminUserService = adminUserService;
        }

        [BindProperty]
        public SubjectFormViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var s = await _subjectService.GetByIdAsync(id);
            if (s == null) return NotFound();

            Input = new SubjectFormViewModel
            {
                SubjectID = s.SubjectID,
                SubjectName = s.SubjectName,
                SubjectCode = s.SubjectCode,
                SubjectLeaderID = s.SubjectLeaderID
            };

            await PopulateTeachersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) 
            {
                await PopulateTeachersAsync();
                return Page();
            }

            try
            {
                await _subjectService.UpdateAsync(new UpdateSubjectDto
                {
                    SubjectID = Input.SubjectID,
                    SubjectName = Input.SubjectName,
                    SubjectCode = Input.SubjectCode,
                    SubjectLeaderID = Input.SubjectLeaderID
                });
                TempData["Success"] = "Cập nhật môn học thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateTeachersAsync();
                return Page();
            }
        }

        private async Task PopulateTeachersAsync()
        {
            var users = await _adminUserService.GetAllUsersAsync();
            Input.Teachers = users.Where(u => u.Roles.Contains("Teacher")).ToList();
        }
    }
}
