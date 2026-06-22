using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class EmbeddingRepository : GenericRepository<Embedding>, IEmbeddingRepository
    {
        public EmbeddingRepository(AppDbContext context) : base(context) { }

        public async Task<Embedding?> GetByChunkIdAsync(int chunkId)
            => await _context.Embeddings.FirstOrDefaultAsync(e => e.ChunkID == chunkId);
    }
}
