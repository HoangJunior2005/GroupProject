using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class ChapterRepository : GenericRepository<Chapter>, IChapterRepository
    {
        public ChapterRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Chapter>> GetAllAsync()
            => await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.Documents)
                .OrderBy(c => c.Subject.SubjectName)
                .ThenBy(c => c.ChapterNumber)
                .ToListAsync();

        public async Task<IEnumerable<Chapter>> GetBySubjectIdAsync(int subjectId)
            => await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.Documents)
                .Where(c => c.SubjectID == subjectId)
                .OrderBy(c => c.ChapterNumber)
                .ToListAsync();

        public async Task<Chapter?> GetWithDocumentsAsync(int chapterId)
            => await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.ChapterID == chapterId);

        public async Task<bool> IsChapterNumberExistsAsync(int subjectId, int chapterNumber, int? excludeId = null)
            => await _context.Chapters
                .AnyAsync(c => c.SubjectID == subjectId
                            && c.ChapterNumber == chapterNumber
                            && (!excludeId.HasValue || c.ChapterID != excludeId));
    }
}
