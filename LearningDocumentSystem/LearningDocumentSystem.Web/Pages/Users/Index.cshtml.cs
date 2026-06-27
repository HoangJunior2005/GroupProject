using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Users
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly IAdminUserService _adminUserService;
        private readonly ISubjectService _subjectService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAdminUserService adminUserService, ISubjectService subjectService, ILogger<IndexModel> logger)
        {
            _adminUserService = adminUserService;
            _subjectService = subjectService;
            _logger = logger;
        }

        public UserRoleManageViewModel ModelData { get; set; } = new();

        public async Task OnGetAsync()
        {
            var users = await _adminUserService.GetAllUsersAsync();
            var roles = await _adminUserService.GetAllRolesAsync();
            var subjects = await _subjectService.GetAllAsync();

            ModelData = new UserRoleManageViewModel
            {
                Roles = roles,
                AllSubjects = subjects,
                Users = users.Select(u => new UserRoleItemViewModel
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CanUpload = u.CanUpload,
                    Roles = u.Roles,
                    AssignedRoleIds = roles
                        .Where(r => u.Roles.Contains(r.RoleName))
                        .Select(r => r.RoleID)
                        .ToList(),
                    AssignedSubjectIds = subjects
                        .Where(s => s.SubjectLeaderID == u.UserID)
                        .Select(s => s.SubjectID)
                        .ToList()
                })
            };
        }

        // AJAX POST handler to assign subjects
        public async Task<IActionResult> OnPostAssignSubjectsAsync(int userId, [FromForm] List<int> subjectIds)
        {
            try
            {
                var allSubjects = await _subjectService.GetAllAsync();
                
                // Get subjects currently assigned to this user
                var currentlyAssigned = allSubjects.Where(s => s.SubjectLeaderID == userId).ToList();
                
                // 1. Remove user from subjects they no longer manage
                foreach (var subject in currentlyAssigned)
                {
                    if (!subjectIds.Contains(subject.SubjectID))
                    {
                        await _subjectService.AssignLeaderAsync(subject.SubjectID, null);
                    }
                }

                // 2. Assign user to selected subjects
                foreach (var subjectId in subjectIds)
                {
                    var subject = allSubjects.FirstOrDefault(s => s.SubjectID == subjectId);
                    if (subject != null && subject.SubjectLeaderID != userId)
                    {
                        await _subjectService.AssignLeaderAsync(subject.SubjectID, userId);
                    }
                }

                return new JsonResult(new { success = true, message = "Cập nhật môn học thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subjects for user {UserId}.", userId);
                return new BadRequestObjectResult(new { error = ex.Message });
            }
        }
    }
}
