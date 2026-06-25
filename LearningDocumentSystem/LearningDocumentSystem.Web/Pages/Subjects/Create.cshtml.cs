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
    public class CreateModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IAdminUserService _adminUserService;

        public CreateModel(ISubjectService subjectService, IAdminUserService adminUserService)
        {
            _subjectService = subjectService;
            _adminUserService = adminUserService;
        }

        [BindProperty]
        public SubjectFormViewModel Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            await PopulateTeachersAsync();
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
                await _subjectService.CreateAsync(new CreateSubjectDto
                {
                    SubjectName = Input.SubjectName,
                    SubjectCode = Input.SubjectCode,
                    SubjectLeaderID = Input.SubjectLeaderID
                });
                TempData["Success"] = "Tạo môn học thành công.";
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
