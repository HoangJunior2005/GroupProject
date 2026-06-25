using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
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
    public class EditModel : PageModel
    {
        private readonly IAdminUserService _adminUserService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IAdminUserService adminUserService, ILogger<EditModel> logger)
        {
            _adminUserService = adminUserService;
            _logger = logger;
        }

        [BindProperty]
        public int UserID { get; set; }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        public int SelectedRoleId { get; set; }

        [BindProperty]
        public bool CanUpload { get; set; }

        public IEnumerable<RoleDto> Roles { get; set; } = [];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var users = await _adminUserService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.UserID == id);
            if (user == null) return NotFound();

            UserID = user.UserID;
            Username = user.Username;
            FullName = user.FullName;
            CanUpload = user.CanUpload;

            Roles = await _adminUserService.GetAllRolesAsync();
            
            // Tìm role ID tương ứng với role hiện tại
            var userRole = user.Roles.FirstOrDefault();
            var role = Roles.FirstOrDefault(r => r.RoleName == userRole);
            if (role != null)
            {
                SelectedRoleId = role.RoleID;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var users = await _adminUserService.GetAllUsersAsync();
                var user = users.FirstOrDefault(u => u.UserID == UserID);
                if (user != null)
                {
                    Roles = await _adminUserService.GetAllRolesAsync();
                    var userRole = user.Roles.FirstOrDefault();
                    var role = Roles.FirstOrDefault(r => r.RoleName == userRole);
                    if (role != null)
                    {
                        await _adminUserService.UpdateUserRolesAsync(UserID, new List<int> { role.RoleID }, CanUpload);
                    }
                }
                TempData["Success"] = "Cập nhật thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}.", UserID);
                ModelState.AddModelError(string.Empty, ex.Message);
                Roles = await _adminUserService.GetAllRolesAsync();
                return Page();
            }
        }
    }
}
