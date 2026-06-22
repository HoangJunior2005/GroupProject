using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface ISubjectRepository : IGenericRepository<Subject>
    {
        Task<Subject?> GetWithChaptersAsync(int subjectId);
        Task<bool> IsCodeExistsAsync(string code, int? excludeId = null);
        Task<IEnumerable<Subject>> GetAllActiveAsync();
    }
}
