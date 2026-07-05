using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Web.Pages.Chat
{
    [Authorize(Roles = AppConstants.RoleStudent)]
    public class IndexModel : PageModel
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IChatService chatService,
            ISubjectService subjectService,
            ILogger<IndexModel> logger)
        {
            _chatService = chatService;
            _subjectService = subjectService;
            _logger = logger;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = [];

        // Lấy UserID từ claims
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("UserID") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }

        public async Task OnGetAsync()
        {
            Subjects = await _subjectService.GetAllAsync();
        }

        // ────────────────────────────────────────────────────────────
        // CHAT ASK (giữ nguyên)
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostAskAsync(string question, int? subjectId, int? chapterId, string? modelProvider)
        {
            if (string.IsNullOrWhiteSpace(question))
                return new JsonResult(new { answer = "Vui lòng nhập câu hỏi hợp lệ." });

            try
            {
                var result = await _chatService.AskQuestionAsync(question.Trim(), subjectId, chapterId, modelProvider);
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat question in Razor Page.");
                return new JsonResult(new { answer = "Đã xảy ra lỗi hệ thống khi xử lý câu hỏi của bạn. Vui lòng thử lại sau." });
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Tạo phiên mới
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostCreateSessionAsync(string? title, int? subjectId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { error = "Không xác định được người dùng." });

            try
            {
                var session = await _chatService.CreateSessionAsync(userId, title, subjectId);
                return new JsonResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat session.");
                return new JsonResult(new { error = "Không thể tạo phiên mới." });
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Lấy danh sách phiên
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetSessionsAsync()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new List<ChatSessionDto>());

            try
            {
                var sessions = await _chatService.GetUserSessionsAsync(userId);
                return new JsonResult(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat sessions.");
                return new JsonResult(new List<ChatSessionDto>());
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Lấy messages của phiên
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetSessionMessagesAsync(int sessionId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new List<ChatMessageDto>());

            try
            {
                var messages = await _chatService.GetSessionMessagesAsync(sessionId, userId);
                return new JsonResult(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading session messages for session {SessionId}.", sessionId);
                return new JsonResult(new List<ChatMessageDto>());
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Lưu cặp messages (user + assistant) sau khi AI trả lời
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostSaveMessagesAsync(
            int sessionId,
            string userContent,
            string assistantContent,
            string? sourcesJson,
            string? providerName,
            string? modelName,
            double? executionTimeMs,
            int? promptTokens,
            int? completionTokens)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { ok = false });

            try
            {
                List<ChatSourceDto>? sources = null;
                if (!string.IsNullOrWhiteSpace(sourcesJson))
                {
                    try 
                    { 
                        sources = JsonSerializer.Deserialize<List<ChatSourceDto>>(
                            sourcesJson, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ); 
                    }
                    catch { /* ignore */ }
                }

                await _chatService.SaveMessagesAsync(sessionId, userContent, assistantContent, sources, providerName, modelName, executionTimeMs, promptTokens, completionTokens);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving messages for session {SessionId}.", sessionId);
                return new JsonResult(new { ok = false });
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Cập nhật feedback (Thumbs Up/Down)
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostFeedbackAsync(int messageId, int feedback)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { ok = false });

            try
            {
                await _chatService.UpdateMessageFeedbackAsync(messageId, userId, feedback);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message feedback {MessageId}.", messageId);
                return new JsonResult(new { ok = false });
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Xóa phiên
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostDeleteSessionAsync(int sessionId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { ok = false });

            try
            {
                await _chatService.DeleteSessionAsync(sessionId, userId);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}.", sessionId);
                return new JsonResult(new { ok = false });
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Đổi tên phiên
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostUpdateSessionTitleAsync(int sessionId, string title)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { ok = false });

            try
            {
                await _chatService.UpdateSessionTitleAsync(sessionId, userId, title);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session title {SessionId}.", sessionId);
                return new JsonResult(new { ok = false });
            }
        }

        // ────────────────────────────────────────────────────────────
        // SESSION: Đổi môn học của phiên
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostUpdateSessionSubjectAsync(int sessionId, int? subjectId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { ok = false });

            try
            {
                await _chatService.UpdateSessionSubjectAsync(sessionId, userId, subjectId);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session subject {SessionId}.", sessionId);
                return new JsonResult(new { ok = false });
            }
        }
    }
}
