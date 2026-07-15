using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface ISystemChunkSettingRepository : IGenericRepository<SystemChunkSetting>
    {
        Task<SystemChunkSetting?> GetByUserIdAsync(int userId);
        Task UpsertAsync(SystemChunkSetting setting);
    }
}
