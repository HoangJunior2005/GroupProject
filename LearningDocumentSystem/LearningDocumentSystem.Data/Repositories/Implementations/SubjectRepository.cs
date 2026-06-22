using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
    {
        public SubjectRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Subject>> GetAllAsync()
            => await _context.Subjects
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Documents)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<Subject?> GetWithChaptersAsync(int subjectId)
            => await _context.Subjects
                .Include(s => s.Chapters.OrderBy(c => c.ChapterNumber))
                    .ThenInclude(c => c.Documents)
                .FirstOrDefaultAsync(s => s.SubjectID == subjectId);

        public async Task<bool> IsCodeExistsAsync(string code, int? excludeId = null)
            => await _context.Subjects
                .AnyAsync(s => s.SubjectCode == code && (!excludeId.HasValue || s.SubjectID != excludeId));

        public async Task<IEnumerable<Subject>> GetAllActiveAsync()
            => await GetAllAsync();
    }
}
