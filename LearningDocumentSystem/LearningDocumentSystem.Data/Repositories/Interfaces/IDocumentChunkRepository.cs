using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IDocumentChunkRepository : IGenericRepository<DocumentChunk>
    {
        Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(int documentId);
        Task<int> CountByDocumentIdAsync(int documentId);
        Task DeleteByDocumentIdAsync(int documentId);
        Task<IEnumerable<DocumentChunk>> GetChunksForRAGAsync(int? subjectId, int? chapterId);
        Task<Dictionary<int, int>> GetChunkCountsAsync(IEnumerable<int> documentIds);
    }
}
