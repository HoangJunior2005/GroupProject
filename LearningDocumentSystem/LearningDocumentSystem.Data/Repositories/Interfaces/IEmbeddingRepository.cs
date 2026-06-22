using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IEmbeddingRepository : IGenericRepository<Embedding>
    {
        Task<Embedding?> GetByChunkIdAsync(int chunkId);
    }
}
