using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class SystemChunkSettingRepository : GenericRepository<SystemChunkSetting>, ISystemChunkSettingRepository
    {
        public SystemChunkSettingRepository(AppDbContext context) : base(context) { }

        public async Task<SystemChunkSetting?> GetByUserIdAsync(int userId)
            => await _dbSet.FirstOrDefaultAsync(s => s.UserId == userId);

        public async Task UpsertAsync(SystemChunkSetting setting)
        {
            var existing = await _dbSet.FindAsync(setting.UserId);
            if (existing == null)
            {
                await _dbSet.AddAsync(setting);
            }
            else
            {
                existing.Strategy       = setting.Strategy;
                existing.ChunkSize      = setting.ChunkSize;
                existing.ChunkOverlap   = setting.ChunkOverlap;
                existing.MinChunkLength = setting.MinChunkLength;
                existing.UpdatedAt      = DateTime.UtcNow;
                _dbSet.Update(existing);
            }
        }
    }
}
