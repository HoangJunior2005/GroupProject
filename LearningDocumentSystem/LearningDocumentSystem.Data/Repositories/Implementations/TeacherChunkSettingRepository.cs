using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class TeacherChunkSettingRepository : GenericRepository<TeacherChunkSetting>, ITeacherChunkSettingRepository
    {
        public TeacherChunkSettingRepository(AppDbContext context) : base(context) { }

        public async Task<TeacherChunkSetting?> GetByTeacherIdAsync(int teacherId)
            => await _dbSet.FirstOrDefaultAsync(s => s.TeacherId == teacherId);

        public async Task UpsertAsync(TeacherChunkSetting setting)
        {
            var existing = await _dbSet.FindAsync(setting.TeacherId);
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
