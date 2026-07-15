using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class PackagePlanRepository : GenericRepository<PackagePlan>, IPackagePlanRepository
    {
        public PackagePlanRepository(AppDbContext context) : base(context) { }

        public async Task<PackagePlan?> GetByCodeAsync(string code)
            => await _context.PackagePlans.FirstOrDefaultAsync(p => p.Code == code);

        public async Task<IReadOnlyList<PackagePlan>> GetActivePlansAsync()
            => await _context.PackagePlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Code)
                .ToListAsync();
    }
}
