using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface ITeacherChunkSettingRepository : IGenericRepository<TeacherChunkSetting>
    {
        Task<TeacherChunkSetting?> GetByTeacherIdAsync(int teacherId);
        Task UpsertAsync(TeacherChunkSetting setting);
    }
}
