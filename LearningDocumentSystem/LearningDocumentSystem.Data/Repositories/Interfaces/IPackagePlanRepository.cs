using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IPackagePlanRepository : IGenericRepository<PackagePlan>
    {
        Task<PackagePlan?> GetByCodeAsync(string code);
        Task<IReadOnlyList<PackagePlan>> GetActivePlansAsync();
    }
}
