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
using System.Diagnostics;
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
            INotificationService notificationService)
        {
            _uow              = uow;
            _mapper           = mapper;
            _fileService      = fileService;
            _chunkingService  = chunkingService;
            _embeddingService = embeddingService;
            _geminiService    = geminiService;
            _logger           = logger;
            _notificationService = notificationService;
        }

        public void SetUploadPath(string path) => _uploadPath = path;

        public async Task<(IEnumerable<DocumentDto> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, string? status, int? teacherId, int page, int pageSize)
        {
            var (items, total) = await _uow.Documents.GetPagedAsync(
                keyword, subjectId, chapterId, status, teacherId, page, pageSize);
            return (_mapper.Map<IEnumerable<DocumentDto>>(items), total);
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
            // Step 1: Lưu file vật lý
            await _uow.BeginTransactionAsync();
            try
            {
                storageName = await _fileService.SaveFileAsync(file, _uploadPath);

                // Step 2: Tạo Document record
                var document = new Document
                {
                    ChapterID      = chapterId,
                    Title          = title,
                    FileType       = FileHelper.GetFileType(file.FileName),
                    StoragePath    = storageName,
                    FileSizeInBytes = file.Length,
                    IndexStatus    = AppConstants.StatusPending,
                    UploadedBy       = uploadedByUserId,
                    UploadedAt       = DateTime.UtcNow,
                    FileHash         = hash,
                    OriginalFileName = originalFileName
                };
                await _uow.Documents.AddAsync(document);
                await _uow.SaveChangesAsync();

                // Step 3: Cập nhật status → Processing
                await _uow.Documents.UpdateStatusAsync(document.DocumentID, AppConstants.StatusProcessing);
                await _uow.SaveChangesAsync();

                // Step 4: Chunking + Embedding — đo thời gian cho IndexBenchmarkLog
                var fullPath = Path.Combine(_uploadPath, storageName);
                var indexingStopwatch = Stopwatch.StartNew();
                var chunks   = await _chunkingService.ExtractChunksAsync(fullPath, document.FileType);

                // Step 5: Lưu chunks + embeddings
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

                // Step 7: Quét mâu thuẫn kiến thức bằng AI (Cấp độ 4) - Đã tạm ẩn để tránh lỗi upload
                /*
                try
                {
                    await CheckForConflictsAsync(document, chapter.SubjectID, chunkEntities, embeddings);
                }
                catch (InvalidOperationException)
                {
                    throw; // Rethrow to abort transaction and reject upload
                }
                catch (Exception conflictEx)
                {
                    _logger.LogError(conflictEx, "Unexpected error scanning document conflicts for document {DocId}.", document.DocumentID);
                }
                */

                // Step 8: Cập nhật status → Indexed
                await _uow.Documents.UpdateStatusAsync(document.DocumentID, AppConstants.StatusIndexed);
                indexingStopwatch.Stop();
                await _uow.SaveChangesAsync();

                // Step 9: Ghi IndexBenchmarkLog
                try
                {
                    double avgChunkSize = chunkEntities.Count > 0
                        ? chunkEntities.Average(c => (double)c.ContentText.Length)
                        : 0;
                    var indexLog = new IndexBenchmarkLog
                    {
                        DocumentID = document.DocumentID,
                        ChunkingStrategy = "Paragraph",
                        EmbeddingModel = "TF-IDF",
                        TotalChunksGenerated = chunkEntities.Count,
                        ProcessingTimeMs = indexingStopwatch.Elapsed.TotalMilliseconds,
                        AverageChunkSize = Math.Round(avgChunkSize, 1),
                        ExecutedAt = DateTime.UtcNow
                    };
                    await _uow.BenchmarkLogs.AddIndexLogAsync(indexLog);
                    await _uow.SaveChangesAsync();
                    _logger.LogInformation("IndexBenchmarkLog saved: doc={DocId}, chunks={Count}, ms={Ms}",
                        document.DocumentID, chunkEntities.Count, indexingStopwatch.Elapsed.TotalMilliseconds);
                }
                catch (Exception logEx)
                {
                    // Log lỗi nhưng không làm ảnh hưởng việc upload
                    _logger.LogWarning(logEx, "Failed to write IndexBenchmarkLog for document {DocId}.", document.DocumentID);
                }

                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Document uploaded: {Title}, {ChunkCount} chunks, {EmbCount} embeddings.",
                    title, chunkEntities.Count, embeddings.Count);

                // Reload với full details để map
                var result = await _uow.Documents.GetWithDetailsAsync(document.DocumentID);
                var dto = _mapper.Map<DocumentDto>(result!);
                await _notificationService.SendNotificationAsync("DocumentChanged", new { action = "Upload", document = dto });
                return dto;
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
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
            var totalDocs    = await _uow.Documents.CountAsync();
            var totalChunks  = await _uow.DocumentChunks.CountAsync();
            var totalSubjects = await _uow.Subjects.CountAsync();
            var totalUsers   = await _uow.Users.CountAsync();
            var indexed      = await _uow.Documents.CountByStatusAsync(AppConstants.StatusIndexed);
            var pending      = await _uow.Documents.CountByStatusAsync(AppConstants.StatusPending);
            var processing   = await _uow.Documents.CountByStatusAsync(AppConstants.StatusProcessing);
            var failed       = await _uow.Documents.CountByStatusAsync(AppConstants.StatusFailed);

            var (recent, _) = await _uow.Documents.GetPagedAsync(null, null, null, null, null, 1, 5);

            // Monthly uploads - last 12 months
            var now = DateTime.UtcNow;
            var twelveMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var allDocs = await _uow.Documents.FindAsync(d => d.UploadedAt >= twelveMonthsAgo);
            var uploadGroups = allDocs
                .GroupBy(d => new { d.UploadedAt.Year, d.UploadedAt.Month })
                .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Count());

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
            var allUsers = await _uow.Users.FindAsync(u => u.CreatedAt >= twelveMonthsAgo);
            var userGroups = allUsers
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Count());

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
                RecentDocuments   = _mapper.Map<List<DocumentDto>>(recent),
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

            foreach (var newChunk in newChunks)
            {
                var newEmb = newEmbeddings.FirstOrDefault(e => e.ChunkID == newChunk.ChunkID);
                if (newEmb == null) continue;

                var newVector = JsonSerializer.Deserialize<float[]>(newEmb.VectorData);
                if (newVector == null) continue;

                // Tìm top 3 chunk tương đồng nhất
                var candidateChunks = existingChunks
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
                    .Where(x => x.Score >= 0.15f) // Ngưỡng tương đồng ngữ nghĩa
                    .OrderByDescending(x => x.Score)
                    .Take(3)
                    .ToList();

                foreach (var candidate in candidateChunks)
                {
                    var oldChunk = candidate.Chunk;
                    
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

                    if (!string.IsNullOrWhiteSpace(analysisResult) && !analysisResult.Contains("NO_CONFLICT", StringComparison.OrdinalIgnoreCase))
                    {
                        conflictsList.Add($"Mâu thuẫn với tài liệu \"{oldChunk.Document.Title}\": {analysisResult.Trim()}");
                    }
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
    }
}
