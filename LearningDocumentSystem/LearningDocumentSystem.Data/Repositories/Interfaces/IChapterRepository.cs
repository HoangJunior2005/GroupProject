using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IChapterRepository : IGenericRepository<Chapter>
    {
        Task<IEnumerable<Chapter>> GetBySubjectIdAsync(int subjectId);
        Task<Chapter?> GetWithDocumentsAsync(int chapterId);
        Task<bool> IsChapterNumberExistsAsync(int subjectId, int chapterNumber, int? excludeId = null);
    }
}
