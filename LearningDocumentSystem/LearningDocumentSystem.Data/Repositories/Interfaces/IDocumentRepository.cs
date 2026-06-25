using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IDocumentRepository : IGenericRepository<Document>
    {
        Task<Document?> GetWithDetailsAsync(int documentId);
        Task<IEnumerable<Document>> GetByChapterIdAsync(int chapterId);
        Task<IEnumerable<Document>> SearchAsync(string? keyword, int? subjectId, int? chapterId);
        Task<(IEnumerable<Document> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, string? status, int? teacherId,
            int page, int pageSize);
        Task<int> CountByStatusAsync(string status);
        Task UpdateStatusAsync(int documentId, string status);
    }
}
