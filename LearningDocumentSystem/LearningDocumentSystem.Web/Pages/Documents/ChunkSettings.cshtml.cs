using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace LearningDocumentSystem.Web.Pages.Documents
{
    [Authorize(Policy = "AdminOnly")]
    public class ChunkSettingsModel : PageModel
    {
        private readonly IChunkSettingsService _chunkSettingsService;
        private readonly IDocumentService _documentService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ChunkSettingsModel> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ChunkSettingsModel(
            IChunkSettingsService chunkSettingsService,
            IDocumentService documentService,
            IHubContext<NotificationHub> hubContext,
            ILogger<ChunkSettingsModel> logger,
            IServiceScopeFactory scopeFactory)
        {
            _chunkSettingsService = chunkSettingsService;
            _documentService      = documentService;
            _hubContext           = hubContext;
            _logger               = logger;
            _scopeFactory         = scopeFactory;
        }

        [BindProperty]
        public ChunkSettingsDto Settings { get; set; } = new();

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("UserID") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }

        public async Task OnGetAsync()
        {
            Settings = await _chunkSettingsService.GetGlobalSettingsAsync();
        }

        // ────────────────────────────────────────────────
        // SAVE SETTINGS
        // ────────────────────────────────────────────────
        public async Task<IActionResult> OnPostSaveAsync(
            string strategy, int chunkSize, int chunkOverlap, int minChunkLength)
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
                return new JsonResult(new { ok = false, message = "Không xác định được người dùng." });

            try
            {
                await _chunkSettingsService.SaveSettingsAsync(
                    userId, strategy, chunkSize, chunkOverlap, minChunkLength);

                return new JsonResult(new { ok = true, message = "Đã lưu cấu hình Chunking thành công!" });
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(new { ok = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chunk settings for user {UserId}", userId);
                return new JsonResult(new { ok = false, message = "Lỗi hệ thống khi lưu cấu hình." });
            }
        }

        // ────────────────────────────────────────────────
        // APPLY & RE-CHUNK ALL (background job via SignalR)
        // ────────────────────────────────────────────────
        public async Task<IActionResult> OnPostReChunkAllAsync(
            string strategy, int chunkSize, int chunkOverlap, int minChunkLength)
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
                return new JsonResult(new { ok = false, message = "Không xác định được người dùng." });

            try
            {
                // 1. Lưu cấu hình mới trước
                await _chunkSettingsService.SaveSettingsAsync(
                    userId, strategy, chunkSize, chunkOverlap, minChunkLength);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, message = ex.Message });
            }

            // 3. Chạy background task trong Scope mới để tránh lỗi Disposed DbContext
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedDocService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                // Lấy upload path từ DocumentService dùng WebRootPath
                if (scopedDocService is LearningDocumentSystem.Business.Services.Implementations.DocumentService ds)
                {
                    ds.SetUploadPath(Path.Combine(env.WebRootPath, "uploads"));
                }

                try
                {
                    await _hubContext.Clients.All.SendAsync("ReChunkProgress", new
                    {
                        current = 0, total = -1, message = "Đang bắt đầu Re-chunk...", done = false
                    });

                    await scopedDocService.ReChunkAllDocumentsAsync(null,
                        async (current, total) =>
                        {
                            int pct = (int)(current * 100.0 / total);
                            await _hubContext.Clients.All.SendAsync("ReChunkProgress", new
                            {
                                current, total,
                                percent = pct,
                                message = $"Đang xử lý tài liệu {current}/{total}...",
                                done = false
                            });
                        });

                    await _hubContext.Clients.All.SendAsync("ReChunkProgress", new
                    {
                        current = 0, total = 0, percent = 100,
                        message = "Re-chunk hoàn tất! Tất cả tài liệu đã được phân mảnh lại.",
                        done = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background re-chunk failed");
                    await _hubContext.Clients.All.SendAsync("ReChunkProgress", new
                    {
                        current = 0, total = 0, percent = 0,
                        message = $"❌ Lỗi trong quá trình re-chunk: {ex.Message}",
                        done = true, error = true
                    });
                }
            });

            return new JsonResult(new
            {
                ok = true,
                message = "Đã bắt đầu quá trình Re-chunk. Theo dõi tiến trình bên dưới."
            });
        }
    }
}
