using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Users
{
    [Authorize(Policy = "AdminOnly")]
    public class CreateModel : PageModel
    {
        private readonly IAdminUserService _adminUserService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IAdminUserService adminUserService, ILogger<CreateModel> logger)
        {
            _adminUserService = adminUserService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Email không được để trống")]
            [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Họ tên không được để trống")]
            [MaxLength(100)]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu không được để trống")]
            [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
            [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                await _adminUserService.CreateTeacherAccountAsync(Input.Email, Input.FullName, Input.Password);
                TempData["Success"] = "Tạo tài khoản Giảng viên thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating teacher account.");
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
