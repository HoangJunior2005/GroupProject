using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class DocumentChunkRepository : GenericRepository<DocumentChunk>, IDocumentChunkRepository
    {
        public DocumentChunkRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(int documentId)
            => await _context.DocumentChunks
                .Include(c => c.Embedding)
                .Where(c => c.DocumentID == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();

        public async Task<int> CountByDocumentIdAsync(int documentId)
            => await _context.DocumentChunks.CountAsync(c => c.DocumentID == documentId);

        public async Task DeleteByDocumentIdAsync(int documentId)
        {
            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentID == documentId)
                .ToListAsync();
            _context.DocumentChunks.RemoveRange(chunks);
        }

        public async Task<IEnumerable<DocumentChunk>> GetChunksForRAGAsync(int? subjectId, int? chapterId)
        {
            var query = _context.DocumentChunks
                .Include(c => c.Embedding)
                .Include(c => c.Document).ThenInclude(d => d.Chapter)
                .Where(c => c.Document.IndexStatus == "Indexed")
                .AsQueryable();

            if (subjectId.HasValue)
                query = query.Where(c => c.Document.Chapter.SubjectID == subjectId.Value);

            if (chapterId.HasValue)
                query = query.Where(c => c.Document.ChapterID == chapterId.Value);

            return await query.ToListAsync();
        }
    }
}
