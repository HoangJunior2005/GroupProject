using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LearningDocumentSystem.Common.Constants;

namespace LearningDocumentSystem.Web.Pages.Subjects
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;

        public IndexModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = [];

        public async Task OnGetAsync()
        {
            var allSubjects = await _subjectService.GetAllAsync();
            
            if (User.IsInRole(AppConstants.RoleTeacher))
            {
                var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(currentUserIdStr, out int currentUserId))
                {
                    Subjects = allSubjects.Where(s => s.SubjectLeaderID == currentUserId).ToList();
                }
                else
                {
                    Subjects = new List<SubjectDto>();
                }
            }
            else
            {
                // Admin and Students see all subjects
                Subjects = allSubjects;
            }
        }
    }
}
