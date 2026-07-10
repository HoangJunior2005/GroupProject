using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork       _uow;
        private readonly IMapper           _mapper;
        private readonly IFileService      _fileService;
        private readonly IChunkingService  _chunkingService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IGeminiService    _geminiService;
        private readonly ILogger<DocumentService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IChunkSettingsService _chunkSettingsService;

        // Inject upload path qua constructor (set bởi Web layer)
        private string _uploadPath = string.Empty;

        public DocumentService(
            IUnitOfWork uow,
            IMapper mapper,
            IFileService fileService,
            IChunkingService chunkingService,
            IEmbeddingService embeddingService,
            IGeminiService geminiService,
            ILogger<DocumentService> logger,
            INotificationService notificationService,
            IChunkSettingsService chunkSettingsService)
        {
            _uow              = uow;
            _mapper           = mapper;
            _fileService      = fileService;
            _chunkingService  = chunkingService;
            _embeddingService = embeddingService;
            _geminiService    = geminiService;
            _logger           = logger;
            _notificationService = notificationService;
            _chunkSettingsService = chunkSettingsService;
        }

        public void SetUploadPath(string path) => _uploadPath = path;

        public async Task<(IEnumerable<DocumentDto> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, string? status, int? teacherId, int page, int pageSize)
        {
            var (items, total) = await _uow.Documents.GetPagedAsync(
                keyword, subjectId, chapterId, status, teacherId, page, pageSize);

            var dtos = _mapper.Map<List<DocumentDto>>(items);
            if (dtos.Any())
            {
                var docIds = dtos.Select(d => d.DocumentID).ToList();
                var chunkCounts = await _uow.DocumentChunks.GetChunkCountsAsync(docIds);
                foreach (var dto in dtos)
                {
                    dto.ChunkCount = chunkCounts.GetValueOrDefault(dto.DocumentID, 0);
                }
            }
            return (dtos, total);
        }

        public async Task<DocumentDetailDto?> GetDetailAsync(int id)
        {
            var doc = await _uow.Documents.GetWithDetailsAsync(id);
            if (doc == null) return null;

            var dto = _mapper.Map<DocumentDetailDto>(doc);
            dto.Chunks = _mapper.Map<List<ChunkDto>>(doc.Chunks);

            var conflicts = await _uow.DocumentConflicts.GetByDocumentIdAsync(id);
            dto.Conflicts = _mapper.Map<List<DocumentConflictDto>>(conflicts);

            return dto;
        }

        public async Task<DocumentDto> UploadAsync(
            IFormFile file, int chapterId, string title, int uploadedByUserId)
        {
            // Validate chapter tồn tại
            var chapter = await _uow.Chapters.GetByIdAsync(chapterId)
                ?? throw new NotFoundException("Chapter", chapterId);

            // --- Validate sớm (chỉ query DB, chưa lưu file / chưa gọi AI) ---
            var normalizedTitle = title.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedTitle))
                throw new InvalidOperationException("Tiêu đề tài liệu không được để trống.");

            var isDuplicateTitle = await _uow.Documents.AnyAsync(
                d => d.Title.ToLower() == normalizedTitle);
            if (isDuplicateTitle)
            {
                throw new InvalidOperationException(AppMessages.MsgDuplicateTitle);
            }

            var originalFileName = Path.GetFileName(file.FileName);
            var normalizedFileName = originalFileName.ToLowerInvariant();

            var isDuplicateName = await _uow.Documents.AnyAsync(
                d => d.OriginalFileName.ToLower() == normalizedFileName);
            if (isDuplicateName)
            {
                throw new InvalidOperationException(AppMessages.MsgDuplicateFileName);
            }

            // Tính toán mã băm SHA256 của tệp tin tải lên
            var hash = await ComputeFileHashAsync(file);

            // Kiểm tra trùng lặp nội dung tệp
            var isDuplicate = await _uow.Documents.AnyAsync(d => d.FileHash == hash);
            if (isDuplicate)
            {
                throw new InvalidOperationException("Tài liệu này đã được tải lên hệ thống trước đó (trùng khớp nội dung tệp tin).");
            }

            // --- Từ đây trở đi mới bắt đầu xử lý nặng: lưu file, chunk, embedding, Gemini conflict scan ---
            string? storageName = null;
            Document? document = null;
            // Step 1: Lưu file vật lý
            await _uow.BeginTransactionAsync();
            try
            {
                storageName = await _fileService.SaveFileAsync(file, _uploadPath);

                // Step 2: Tạo Document record
                document = new Document
                {
                    ChapterID      = chapterId,
                    Title          = title,
                    FileType       = FileHelper.GetFileType(file.FileName),
                    StoragePath    = storageName,
                    FileSizeInBytes = file.Length,
                    IndexStatus    = AppConstants.StatusProcessing,
                    UploadedBy       = uploadedByUserId,
                    UploadedAt       = DateTime.UtcNow,
                    FileHash         = hash,
                    OriginalFileName = originalFileName
                };
                await _uow.Documents.AddAsync(document);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                // Gửi thông báo ngay lập tức cho các client
                var docProcessing = await _uow.Documents.GetWithDetailsAsync(document.DocumentID);
                var docProcessingDto = _mapper.Map<DocumentDto>(docProcessing!);
                await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "UploadProcessing", document = docProcessingDto });

                // Step 4: Chunking — dùng settings global (do Admin cấu hình)
                var fullPath = Path.Combine(_uploadPath, storageName);
                var globalSettings = await _chunkSettingsService.GetGlobalSettingsAsync();
                var chunks = await _chunkingService.ExtractChunksAsync(
                    fullPath, document.FileType,
                    globalSettings.Strategy,
                    globalSettings.ChunkSize,
                    globalSettings.ChunkOverlap,
                    globalSettings.MinChunkLength);

                // Step 5: Lưu chunks + embeddings
                // Mở transaction mới cho phần lưu chunks + embeddings
                await _uow.BeginTransactionAsync();
                try
                {
                    var chunkEntities = new List<DocumentChunk>();
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        var (content, pageNum) = chunks[i];
                        var chunk = new DocumentChunk
                        {
                            DocumentID = document.DocumentID,
                            ChunkIndex = i,
                            PageNumber = pageNum,
                            ContentText = content
                        };
                        chunkEntities.Add(chunk);
                    }
                    await _uow.DocumentChunks.AddRangeAsync(chunkEntities);
                    await _uow.SaveChangesAsync();

                    // Step 6: Sinh embedding cho từng chunk
                    var embeddings = new List<Embedding>();
                    foreach (var chunk in chunkEntities)
                    {
                        var vectorJson = await _embeddingService.GenerateEmbeddingAsync(chunk.ContentText);
                        embeddings.Add(new Embedding
                        {
                            ChunkID    = chunk.ChunkID,
                            VectorData = vectorJson,
                            CreatedAt  = DateTime.UtcNow
                        });
                    }
                    await _uow.Embeddings.AddRangeAsync(embeddings);

                    // Step 8: Cập nhật status → Indexed
                    await _uow.Documents.UpdateStatusAsync(document.DocumentID, AppConstants.StatusIndexed);
                    await _uow.SaveChangesAsync();

                    await _uow.CommitAsync();
                }
                catch
                {
                    await _uow.RollbackAsync();
                    throw;
                }

                _logger.LogInformation(
                    "Document uploaded: {Title}, {ChunkCount} chunks.",
                    title, chunks.Count);

                // Reload với full details để map
                var result = await _uow.Documents.GetWithDetailsAsync(document.DocumentID);
                var dto = _mapper.Map<DocumentDto>(result!);
                await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "Upload", document = dto });
                return dto;
            }
            catch (Exception ex)
            {
                if (document != null && document.DocumentID > 0)
                {
                    try
                    {
                        await _uow.Documents.UpdateStatusAsync(document.DocumentID, AppConstants.StatusFailed);
                        await _uow.SaveChangesAsync();
                        await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "StatusUpdate", documentId = document.DocumentID, status = AppConstants.StatusFailed });
                    }
                    catch { /* ignore */ }
                }
                else
                {
                    try
                    {
                        await _uow.RollbackAsync();
                    }
                    catch { /* ignore */ }
                }

                try
                {
                    if (!string.IsNullOrEmpty(storageName))
                    {
                        _fileService.DeleteFile(storageName, _uploadPath);
                    }
                }
                catch { /* ignore physical file deletion errors during rollback */ }
                _logger.LogError(ex, "Upload failed for: {Title}", title);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var doc = await _uow.Documents.GetByIdAsync(id)
                ?? throw new NotFoundException("Document", id);

            // Xóa file vật lý
            _fileService.DeleteFile(doc.StoragePath, _uploadPath);

            // Xóa các mâu thuẫn liên quan để tránh khóa ngoại
            var conflicts = await _uow.DocumentConflicts.FindAsync(dc => dc.DocumentID == id || dc.ConflictingDocumentID == id);
            _uow.DocumentConflicts.RemoveRange(conflicts);

            _uow.Documents.Remove(doc);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Document deleted: {Id}", id);
            await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "Delete", documentId = id });
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var statusCounts = await _uow.Documents.GetStatusCountsAsync();
            var totalDocs    = statusCounts.Values.Sum();
            var totalChunks  = await _uow.DocumentChunks.CountAsync();
            var totalSubjects = await _uow.Subjects.CountAsync();
            var totalUsers   = await _uow.Users.CountAsync();
            var indexed      = statusCounts.GetValueOrDefault(AppConstants.StatusIndexed, 0);
            var pending      = statusCounts.GetValueOrDefault(AppConstants.StatusPending, 0);
            var processing   = statusCounts.GetValueOrDefault(AppConstants.StatusProcessing, 0);
            var failed       = statusCounts.GetValueOrDefault(AppConstants.StatusFailed, 0);

            var (recent, _) = await _uow.Documents.GetPagedAsync(null, null, null, null, null, 1, 5);
            var recentDtos = _mapper.Map<List<DocumentDto>>(recent);
            if (recentDtos.Any())
            {
                var docIds = recentDtos.Select(d => d.DocumentID).ToList();
                var chunkCounts = await _uow.DocumentChunks.GetChunkCountsAsync(docIds);
                foreach (var dto in recentDtos)
                {
                    dto.ChunkCount = chunkCounts.GetValueOrDefault(dto.DocumentID, 0);
                }
            }

            // Monthly uploads - last 12 months
            var now = DateTime.UtcNow;
            var twelveMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var uploadGroups = await _uow.Documents.GetMonthlyUploadsAsync(twelveMonthsAgo);

            var monthlyUploads = new List<DTOs.MonthlyUploadDto>();
            for (int i = 0; i < 12; i++)
            {
                var date = twelveMonthsAgo.AddMonths(i);
                var count = uploadGroups.GetValueOrDefault((date.Year, date.Month), 0);
                monthlyUploads.Add(new DTOs.MonthlyUploadDto
                {
                    Year = date.Year,
                    Month = date.Month,
                    Count = count
                });
            }

            // Monthly registrations - last 12 months
            var userGroups = await _uow.Users.GetMonthlyRegistrationsAsync(twelveMonthsAgo);

            var monthlyRegistrations = new List<DTOs.MonthlyUploadDto>();
            for (int i = 0; i < 12; i++)
            {
                var date = twelveMonthsAgo.AddMonths(i);
                var count = userGroups.GetValueOrDefault((date.Year, date.Month), 0);
                monthlyRegistrations.Add(new DTOs.MonthlyUploadDto
                {
                    Year = date.Year,
                    Month = date.Month,
                    Count = count
                });
            }

            return new DashboardDto
            {
                TotalDocuments    = totalDocs,
                TotalChunks       = totalChunks,
                TotalSubjects     = totalSubjects,
                TotalUsers        = totalUsers,
                IndexedDocuments  = indexed,
                PendingDocuments  = pending,
                ProcessingDocuments = processing,
                FailedDocuments   = failed,
                RecentDocuments   = recentDtos,
                MonthlyUploads    = monthlyUploads,
                MonthlyRegistrations = monthlyRegistrations
            };
        }

        private static async Task<string> ComputeFileHashAsync(IFormFile file)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = file.OpenReadStream();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private async Task CheckForConflictsAsync(Document newDoc, int subjectId, List<DocumentChunk> newChunks, List<Embedding> newEmbeddings)
        {
            // Lấy toàn bộ chunk của các tài liệu khác thuộc CÙNG MÔN HỌC để so sánh
            var existingChunks = await _uow.DocumentChunks.GetChunksForRAGAsync(subjectId, null);
            
            // Loại bỏ các chunk thuộc chính tài liệu mới upload
            existingChunks = existingChunks.Where(ec => ec.DocumentID != newDoc.DocumentID).ToList();

            if (!existingChunks.Any()) return;

            var conflictsList = new List<string>();

            var allCandidates = new List<(DocumentChunk NewChunk, DocumentChunk OldChunk, float Score)>();

            foreach (var newChunk in newChunks)
            {
                var newEmb = newEmbeddings.FirstOrDefault(e => e.ChunkID == newChunk.ChunkID);
                if (newEmb == null) continue;

                var newVector = JsonSerializer.Deserialize<float[]>(newEmb.VectorData);
                if (newVector == null) continue;

                var bestMatch = existingChunks
                    .Select(ec => {
                        float[]? ecVector = null;
                        if (ec.Embedding != null && !string.IsNullOrWhiteSpace(ec.Embedding.VectorData))
                        {
                            try { ecVector = JsonSerializer.Deserialize<float[]>(ec.Embedding.VectorData); }
                            catch { /* ignore */ }
                        }
                        float score = ecVector != null ? DotProduct(newVector, ecVector) : 0f;
                        return new { Chunk = ec, Score = score };
                    })
                    .Where(x => x.Score >= 0.85f) // Chỉ kiểm tra khi có độ tương đồng cực cao
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();

                if (bestMatch != null)
                {
                    allCandidates.Add((newChunk, bestMatch.Chunk, bestMatch.Score));
                }
            }

            // Chọn tối đa 2 cặp đoạn có độ tương đồng cao nhất để kiểm tra nhằm tránh lỗi Rate Limit của API
            var topCandidatesToVerify = allCandidates
                .OrderByDescending(c => c.Score)
                .Take(2)
                .ToList();

            foreach (var candidate in topCandidatesToVerify)
            {
                var oldChunk = candidate.OldChunk;
                var newChunk = candidate.NewChunk;
                    
                string prompt = $@"
Bạn là trợ lý học thuật kiểm duyệt thông tin tài liệu.
Nhiệm vụ của bạn là so sánh hai đoạn văn bản dưới đây và xác định xem chúng có chứa bất kỳ mâu thuẫn logic nào hoặc thông tin trái ngược nhau hay không (ví dụ: mâu thuẫn về ngày tháng, công thức, số liệu, định nghĩa hoặc quy chế học vụ).

Đoạn văn bản A (Tài liệu hiện tại - {oldChunk.Document.Title}):
""{oldChunk.ContentText}""

Đoạn văn bản B (Tài liệu mới tải lên - {newDoc.Title}):
""{newChunk.ContentText}""

Quy tắc trả về:
- Nếu KHÔNG CÓ mâu thuẫn thông tin (các thông tin bổ trợ cho nhau hoặc không liên quan), chỉ trả về đúng từ: ""NO_CONFLICT""
- Nếu CÓ mâu thuẫn thông tin, hãy giải thích ngắn gọn điểm mâu thuẫn (Ví dụ: 'Tài liệu cũ ghi lịch thi là 24/06 nhưng tài liệu mới lại ghi lịch thi là 25/06'). Không giải thích dài dòng quá 3 câu.";


                string analysisResult = await _geminiService.GenerateDirectAnswerAsync(prompt);

                // Nếu có lỗi kết nối API thì bỏ qua đoạn này, không ghi nhận là mâu thuẫn
                if (analysisResult.Contains("lỗi khi kết nối", StringComparison.OrdinalIgnoreCase) || 
                    analysisResult.Contains("Lỗi nội bộ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(analysisResult) && !analysisResult.Contains("NO_CONFLICT", StringComparison.OrdinalIgnoreCase))
                {
                    conflictsList.Add($"Mâu thuẫn với tài liệu \"{oldChunk.Document.Title}\": {analysisResult.Trim()}");
                }
            }

            if (conflictsList.Any())
            {
                throw new InvalidOperationException("Phát hiện mâu thuẫn kiến thức! " + string.Join(" | ", conflictsList));
            }
        }

        private static float DotProduct(float[] a, float[] b)
        {
            float sum = 0f;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
                sum += a[i] * b[i];
            return sum;
        }

        // ============================================================
        // RE-CHUNK ALL DOCUMENTS
        // ============================================================
        public async Task ReChunkAllDocumentsAsync(int? teacherId, Func<int, int, Task>? progressCallback = null)
        {
            _logger.LogInformation("ReChunkAll started for teacher {TeacherId}", teacherId);

            // Lấy tất cả tài liệu của teacher này (hoặc toàn bộ nếu teacherId là null)
            var (allDocs, _) = await _uow.Documents.GetPagedAsync(
                keyword: null, subjectId: null, chapterId: null,
                status: null, teacherId: teacherId,
                page: 1, pageSize: int.MaxValue);

            var docs = allDocs.ToList();
            int total = docs.Count;

            if (total == 0)
            {
                _logger.LogInformation("ReChunkAll: no documents found.");
                return;
            }

            // Lấy settings global (do Admin cấu hình)
            var settings = await _chunkSettingsService.GetGlobalSettingsAsync();

            for (int i = 0; i < docs.Count; i++)
            {
                var docDto = docs[i];
                try
                {
                    // 1. Cập nhật status → Processing
                    await _uow.Documents.UpdateStatusAsync(docDto.DocumentID, AppConstants.StatusProcessing);
                    await _uow.SaveChangesAsync();

                    await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "StatusUpdate", documentId = docDto.DocumentID, status = AppConstants.StatusProcessing });

                    // 2. Xóa chunks cũ (cascade xóa embeddings)
                    var oldChunks = (await _uow.DocumentChunks.FindAsync(
                        c => c.DocumentID == docDto.DocumentID)).ToList();
                    _uow.DocumentChunks.RemoveRange(oldChunks);
                    await _uow.SaveChangesAsync();

                    // 3. Xác định full path tập tin
                    var fullPath = Path.Combine(_uploadPath, docDto.StoragePath);
                    if (!File.Exists(fullPath))
                    {
                        _logger.LogWarning("File not found for document {DocId}: {Path}", docDto.DocumentID, fullPath);
                        await _uow.Documents.UpdateStatusAsync(docDto.DocumentID, AppConstants.StatusFailed);
                        await _uow.SaveChangesAsync();

                        await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "StatusUpdate", documentId = docDto.DocumentID, status = AppConstants.StatusFailed });
                        continue;
                    }

                    // 4. Re-chunk với settings mới
                    var chunks = await _chunkingService.ExtractChunksAsync(
                        fullPath, docDto.FileType,
                        settings.Strategy, settings.ChunkSize, settings.ChunkOverlap, settings.MinChunkLength);

                    // 5. Lưu chunks mới
                    var chunkEntities = new List<LearningDocumentSystem.Entities.Models.DocumentChunk>();
                    for (int ci = 0; ci < chunks.Count; ci++)
                    {
                        var (content, pageNum) = chunks[ci];
                        chunkEntities.Add(new LearningDocumentSystem.Entities.Models.DocumentChunk
                        {
                            DocumentID  = docDto.DocumentID,
                            ChunkIndex  = ci,
                            PageNumber  = pageNum,
                            ContentText = content
                        });
                    }
                    await _uow.DocumentChunks.AddRangeAsync(chunkEntities);
                    await _uow.SaveChangesAsync();

                    // 6. Sinh embedding mới
                    var embeddings = new List<LearningDocumentSystem.Entities.Models.Embedding>();
                    foreach (var chunk in chunkEntities)
                    {
                        var vectorJson = await _embeddingService.GenerateEmbeddingAsync(chunk.ContentText);
                        embeddings.Add(new LearningDocumentSystem.Entities.Models.Embedding
                        {
                            ChunkID   = chunk.ChunkID,
                            VectorData = vectorJson,
                            CreatedAt  = DateTime.UtcNow
                        });
                    }
                    await _uow.Embeddings.AddRangeAsync(embeddings);
                    await _uow.SaveChangesAsync();

                    // 7. Cập nhật status → Indexed
                    await _uow.Documents.UpdateStatusAsync(docDto.DocumentID, AppConstants.StatusIndexed);
                    await _uow.SaveChangesAsync();

                    await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "StatusUpdate", documentId = docDto.DocumentID, status = AppConstants.StatusIndexed, chunkCount = chunkEntities.Count });

                    _logger.LogInformation(
                        "ReChunked doc {DocId} ({Index}/{Total}): {ChunkCount} chunks",
                        docDto.DocumentID, i + 1, total, chunkEntities.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error re-chunking document {DocId}", docDto.DocumentID);
                    try
                    {
                        await _uow.Documents.UpdateStatusAsync(docDto.DocumentID, AppConstants.StatusFailed);
                        await _uow.SaveChangesAsync();

                        await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "StatusUpdate", documentId = docDto.DocumentID, status = AppConstants.StatusFailed });
                    }
                    catch { /* ignore secondary errors */ }
                }

                // Báo cáo tiến trình
                if (progressCallback != null)
                    await progressCallback(i + 1, total);
            }

            _logger.LogInformation("ReChunkAll completed: {Total} documents.", total);
        }
    }
}
