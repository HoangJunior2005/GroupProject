using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Data.Repositories.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy-initialized repositories
        private IUserRepository? _users;
        private IRoleRepository? _roles;
        private IUserRoleRepository? _userRoles;
        private ISubjectRepository? _subjects;
        private IChapterRepository? _chapters;
        private IDocumentRepository? _documents;
        private IDocumentChunkRepository? _documentChunks;
        private IEmbeddingRepository? _embeddings;
        private IAllowedEmailRepository? _allowedEmails;
        private IChatSessionRepository? _chatSessions;
        private IDocumentConflictRepository? _documentConflicts;
        private ISystemChunkSettingRepository? _systemChunkSettings;
        private IPaymentTransactionRepository? _paymentTransactions;
        private IPackagePlanRepository? _packagePlans;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users => _users ??= new UserRepository(_context);
        public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
        public IUserRoleRepository UserRoles => _userRoles ??= new UserRoleRepository(_context);
        public ISubjectRepository Subjects => _subjects ??= new SubjectRepository(_context);
        public IChapterRepository Chapters => _chapters ??= new ChapterRepository(_context);
        public IDocumentRepository Documents => _documents ??= new DocumentRepository(_context);
        public IDocumentChunkRepository DocumentChunks => _documentChunks ??= new DocumentChunkRepository(_context);
        public IEmbeddingRepository Embeddings => _embeddings ??= new EmbeddingRepository(_context);
        public IAllowedEmailRepository AllowedEmails => _allowedEmails ??= new AllowedEmailRepository(_context);
        public IChatSessionRepository ChatSessions => _chatSessions ??= new ChatSessionRepository(_context);
        public IDocumentConflictRepository DocumentConflicts => _documentConflicts ??= new DocumentConflictRepository(_context);
        public ISystemChunkSettingRepository SystemChunkSettings => _systemChunkSettings ??= new SystemChunkSettingRepository(_context);
        public IPaymentTransactionRepository PaymentTransactions => _paymentTransactions ??= new PaymentTransactionRepository(_context);
        public IPackagePlanRepository PackagePlans => _packagePlans ??= new PackagePlanRepository(_context);

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync()
            => _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
