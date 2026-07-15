using LearningDocumentSystem.Data.Repositories.Interfaces;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IUserRoleRepository UserRoles { get; }
        ISubjectRepository Subjects { get; }
        IChapterRepository Chapters { get; }
        IDocumentRepository Documents { get; }
        IDocumentChunkRepository DocumentChunks { get; }
        IEmbeddingRepository Embeddings { get; }
        IAllowedEmailRepository AllowedEmails { get; }
        IChatSessionRepository ChatSessions { get; }
        IDocumentConflictRepository DocumentConflicts { get; }
        ITeacherChunkSettingRepository TeacherChunkSettings { get; }
        IPaymentTransactionRepository PaymentTransactions { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
