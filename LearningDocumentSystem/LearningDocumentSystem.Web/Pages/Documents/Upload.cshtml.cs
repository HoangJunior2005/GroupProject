using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Documents
{
    [Authorize(Policy = "TeacherUp")]
    public class UploadModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UploadModel> _logger;

        public UploadModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IWebHostEnvironment env,
            ILogger<UploadModel> logger)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _env = env;
            _logger = logger;
        }

        [BindProperty]
        public DocumentUploadViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? subjectId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var allSubjects = await _subjectService.GetAllAsync();
            var subjects = allSubjects.Where(s => s.SubjectLeaderID == userId).ToList();

            if (!subjects.Any())
            {
                TempData["Error"] = "Bạn không được phân công phụ trách môn học nào (Subject Leader) nên không có quyền upload tài liệu.";
                return RedirectToPage("/Index");
            }

            Input.Subjects = subjects;

            var selectedSubjectId = subjectId;
            if (!selectedSubjectId.HasValue && subjects.Count == 1)
            {
                selectedSubjectId = subjects[0].SubjectID;
            }

            Input.SelectedSubjectId = selectedSubjectId;
            Input.Chapters = selectedSubjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(selectedSubjectId.Value)
                : [];
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                TempData["Error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUploadDropdownsAsync(userId);
                return Page();
            }

            try
            {
                // Kiểm tra quyền: UserId có phải là SubjectLeader của môn học chứa ChapterId này không?
                var chapter = await _chapterService.GetByIdAsync(Input.ChapterId);
                if (chapter == null)
                {
                    ModelState.AddModelError(string.Empty, "Chương không hợp lệ.");
                    await PopulateUploadDropdownsAsync(userId);
                    return Page();
                }

                var subject = await _subjectService.GetByIdAsync(chapter.SubjectID);
                if (subject == null || subject.SubjectLeaderID != userId)
                {
                    TempData["Error"] = "Bạn không có quyền upload tài liệu cho môn học này.";
                    return RedirectToPage("/Index");
                }

                // Thiết lập upload path cho DocumentService (đọc từ environment)
                if (_documentService is Business.Services.Implementations.DocumentService ds)
                {
                    ds.SetUploadPath(Path.Combine(_env.WebRootPath, AppConstants.UploadFolder));
                }

                var doc = await _documentService.UploadAsync(
                    Input.File!, Input.ChapterId, Input.Title, userId);

                TempData["Success"] = AppMessages.MsgUploadSuccess;
                return RedirectToPage("./Detail", new { id = doc.DocumentID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed.");
                if (ex.Message.StartsWith("Phát hiện mâu thuẫn kiến thức!"))
                {
                    var prefix = "Phát hiện mâu thuẫn kiến thức!";
                    var detail = ex.Message.Substring(prefix.Length).Trim();
                    var conflicts = detail.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    ViewData["ConflictErrors"] = conflicts;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                await PopulateUploadDropdownsAsync(userId);
                return Page();
            }
        }

        private async Task PopulateUploadDropdownsAsync(int userId)
        {
            var allSubjects = await _subjectService.GetAllAsync();
            var subjects = allSubjects.Where(s => s.SubjectLeaderID == userId).ToList();
            Input.Subjects = subjects;

            int? selectedSubjectId = Input.SelectedSubjectId;
            if (!selectedSubjectId.HasValue || selectedSubjectId.Value <= 0)
            {
                if (Input.ChapterId > 0)
                {
                    var chapter = await _chapterService.GetByIdAsync(Input.ChapterId);
                    selectedSubjectId = chapter?.SubjectID;
                }
                else if (subjects.Count == 1)
                {
                    selectedSubjectId = subjects[0].SubjectID;
                }
            }

            Input.SelectedSubjectId = selectedSubjectId;
            Input.Chapters = selectedSubjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(selectedSubjectId.Value)
                : [];
        }
    }
}
