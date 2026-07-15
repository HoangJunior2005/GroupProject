using LearningDocumentSystem.Entities.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IPaymentTransactionRepository : IGenericRepository<PaymentTransaction>
    {
        Task<IEnumerable<PaymentTransaction>> GetSuccessfulTransactionsAsync();
    }
}
