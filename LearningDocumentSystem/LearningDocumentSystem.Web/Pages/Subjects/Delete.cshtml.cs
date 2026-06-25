using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Subjects
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ISubjectService _subjectService;

        public DeleteModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                await _subjectService.DeleteAsync(id);
                TempData["Success"] = "Xóa môn học thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage("./Index");
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("./Index");
        }
    }
}
