using System;
using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(AppDbContext context) : base(context) { }

        public async Task<Document?> GetWithDetailsAsync(int documentId)
            => await _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .Include(d => d.Chunks).ThenInclude(c => c.Embedding)
                .FirstOrDefaultAsync(d => d.DocumentID == documentId);

        public async Task<IEnumerable<Document>> GetByChapterIdAsync(int chapterId)
            => await _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .Where(d => d.ChapterID == chapterId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

        public async Task<IEnumerable<Document>> SearchAsync(string? keyword, int? subjectId, int? chapterId)
        {
            var query = _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(d => d.Title.Contains(keyword));
            if (subjectId.HasValue)
                query = query.Where(d => d.Chapter.SubjectID == subjectId);
            if (chapterId.HasValue)
                query = query.Where(d => d.ChapterID == chapterId);

            return await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
        }

        public async Task<(IEnumerable<Document> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, string? status, int page, int pageSize)
        {
            var query = _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .Include(d => d.Chunks)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(d => d.Title.Contains(keyword));
            if (subjectId.HasValue)
                query = query.Where(d => d.Chapter.SubjectID == subjectId);
            if (chapterId.HasValue)
                query = query.Where(d => d.ChapterID == chapterId);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.IndexStatus == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<int> CountByStatusAsync(string status)
            => await _context.Documents.CountAsync(d => d.IndexStatus == status);

        public async Task UpdateStatusAsync(int documentId, string status)
        {
            var doc = await _context.Documents.FindAsync(documentId);
            if (doc != null)
            {
                doc.IndexStatus = status;
                doc.IndexedAt = string.Equals(status, "Indexed", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.UtcNow
                    : null;
                _context.Documents.Update(doc);
            }
        }
    }
}
