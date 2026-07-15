using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class PaymentTransactionRepository : GenericRepository<PaymentTransaction>, IPaymentTransactionRepository
    {
        public PaymentTransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PaymentTransaction>> GetSuccessfulTransactionsAsync()
        {
            return await _dbSet
                .Include(t => t.User)
                .Where(t => t.IsSuccess)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
