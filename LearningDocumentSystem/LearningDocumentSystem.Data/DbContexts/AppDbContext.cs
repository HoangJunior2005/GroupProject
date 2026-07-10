using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.DbContexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets - tương ứng với từng bảng trong DB
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<Chapter> Chapters { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;
        public DbSet<Embedding> Embeddings { get; set; } = null!;
        public DbSet<AllowedEmail> AllowedEmails { get; set; } = null!;
        public DbSet<ChatSession> ChatSessions { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<DocumentConflict> DocumentConflicts { get; set; } = null!;
        public DbSet<TeacherChunkSetting> TeacherChunkSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // BẢNG Users
            // ============================================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.UserID);
                entity.Property(u => u.UserID).UseIdentityColumn();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(100).IsUnicode(true);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.CanUpload).HasDefaultValue(true);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ============================================================
            // BẢNG AllowedEmails
            // ============================================================
            modelBuilder.Entity<AllowedEmail>(entity =>
            {
                entity.ToTable("AllowedEmails");
                entity.HasKey(ae => ae.Id);
                entity.Property(ae => ae.Id).UseIdentityColumn();
                entity.Property(ae => ae.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(ae => ae.Email).IsUnique();
                entity.Property(ae => ae.IsUsed).HasDefaultValue(false);
                entity.Property(ae => ae.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ============================================================
            // BẢNG Roles
            // ============================================================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(r => r.RoleID);
                entity.Property(r => r.RoleID).UseIdentityColumn();
                entity.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
                entity.HasIndex(r => r.RoleName).IsUnique();
            });

            // ============================================================
            // BẢNG UserRoles - Composite PK + CASCADE DELETE
            // ============================================================
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(ur => new { ur.UserID, ur.RoleID }); // Composite PK

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserID)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa User → xóa UserRoles

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleID)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa Role → xóa UserRoles

                // Index tối ưu hóa query theo UserID
                entity.HasIndex(ur => ur.UserID).HasDatabaseName("IX_UserRoles_UserID");
            });

            // ============================================================
            // BẢNG Subjects
            // ============================================================
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("Subjects");
                entity.HasKey(s => s.SubjectID);
                entity.Property(s => s.SubjectID).UseIdentityColumn();
                entity.Property(s => s.SubjectName).IsRequired().HasMaxLength(255).IsUnicode(true);
                entity.Property(s => s.SubjectCode).IsRequired().HasMaxLength(50);
                entity.HasIndex(s => s.SubjectCode).IsUnique();
                entity.Property(s => s.CreatedAt).HasDefaultValueSql("GETDATE()");

                // FK -> Users (SubjectLeader)
                entity.HasOne(s => s.SubjectLeader)
                    .WithMany()
                    .HasForeignKey(s => s.SubjectLeaderID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // BẢNG Chapters - CASCADE từ Subjects
            // ============================================================
            modelBuilder.Entity<Chapter>(entity =>
            {
                entity.ToTable("Chapters");
                entity.HasKey(c => c.ChapterID);
                entity.Property(c => c.ChapterID).UseIdentityColumn();
                entity.Property(c => c.ChapterName).IsRequired().HasMaxLength(255).IsUnicode(true);
                entity.Property(c => c.ChapterNumber).IsRequired();

                entity.HasOne(c => c.Subject)
                    .WithMany(s => s.Chapters)
                    .HasForeignKey(c => c.SubjectID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => c.SubjectID).HasDatabaseName("IX_Chapters_SubjectID");
            });

            // ============================================================
            // BẢNG Documents
            // ============================================================
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");
                entity.HasKey(d => d.DocumentID);
                entity.Property(d => d.DocumentID).UseIdentityColumn();
                entity.Property(d => d.Title).IsRequired().HasMaxLength(255).IsUnicode(true);
                entity.Property(d => d.FileType).IsRequired().HasMaxLength(10);
                entity.Property(d => d.StoragePath).IsRequired().HasMaxLength(2000);
                entity.Property(d => d.FileSizeInBytes).IsRequired();
                entity.Property(d => d.IndexStatus).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(d => d.UploadedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(d => d.IndexedAt).HasColumnType("datetime2");
                entity.Property(d => d.FileHash).HasMaxLength(64).IsRequired(false);
                entity.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(255);

                // FK → Chapters (CASCADE)
                entity.HasOne(d => d.Chapter)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(d => d.ChapterID)
                    .OnDelete(DeleteBehavior.Cascade);

                // FK → Users (NO CASCADE - tránh conflict)
                entity.HasOne(d => d.UploadedByUser)
                    .WithMany(u => u.Documents)
                    .HasForeignKey(d => d.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(d => d.ChapterID).HasDatabaseName("IX_Documents_ChapterID");
                entity.HasIndex(d => d.UploadedBy).HasDatabaseName("IX_Documents_UploadedBy");
                entity.HasIndex(d => d.FileHash).HasDatabaseName("IX_Documents_FileHash");
                entity.HasIndex(d => d.OriginalFileName)
                    .IsUnique()
                    .HasDatabaseName("IX_Documents_OriginalFileName");
            });

            // ============================================================
            // BẢNG DocumentChunks - CASCADE từ Documents
            // ============================================================
            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.ToTable("DocumentChunks");
                entity.HasKey(c => c.ChunkID);
                entity.Property(c => c.ChunkID).UseIdentityColumn();
                entity.Property(c => c.ChunkIndex).IsRequired();
                entity.Property(c => c.PageNumber).IsRequired(false);
                entity.Property(c => c.ContentText).IsRequired().HasColumnType("nvarchar(max)");

                entity.HasOne(c => c.Document)
                    .WithMany(d => d.Chunks)
                    .HasForeignKey(c => c.DocumentID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => c.DocumentID).HasDatabaseName("IX_Chunks_DocumentID");
            });

            // ============================================================
            // BẢNG Embeddings - quan hệ 1-1 với DocumentChunks
            // ============================================================
            modelBuilder.Entity<Embedding>(entity =>
            {
                entity.ToTable("Embeddings");
                entity.HasKey(e => e.EmbeddingID);
                entity.Property(e => e.EmbeddingID).UseIdentityColumn();
                entity.Property(e => e.VectorData).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                // 1-1 relationship với DocumentChunk
                entity.HasOne(e => e.Chunk)
                    .WithOne(c => c.Embedding)
                    .HasForeignKey<Embedding>(e => e.ChunkID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ChunkID).IsUnique();
            });

            // ============================================================
            // BẢNG ChatSessions
            // ============================================================
            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.ToTable("ChatSessions");
                entity.HasKey(cs => cs.SessionID);
                entity.Property(cs => cs.SessionID).UseIdentityColumn();
                entity.Property(cs => cs.Title).IsRequired().HasMaxLength(255).IsUnicode(true);
                entity.Property(cs => cs.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(cs => cs.UpdatedAt).HasDefaultValueSql("GETDATE()");

                // FK → Users (RESTRICT - không cascade xóa user sẽ giữ session)
                entity.HasOne(cs => cs.User)
                    .WithMany(u => u.ChatSessions)
                    .HasForeignKey(cs => cs.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cs => cs.UserID).HasDatabaseName("IX_ChatSessions_UserID");
                entity.HasIndex(cs => cs.UpdatedAt).HasDatabaseName("IX_ChatSessions_UpdatedAt");
            });

            // ============================================================
            // BẢNG ChatMessages
            // ============================================================
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("ChatMessages");
                entity.HasKey(cm => cm.MessageID);
                entity.Property(cm => cm.MessageID).UseIdentityColumn();
                entity.Property(cm => cm.Role).IsRequired().HasMaxLength(20);
                entity.Property(cm => cm.Content).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(cm => cm.SourcesJson).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(cm => cm.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(cm => cm.ProviderName).HasMaxLength(50).IsRequired(false);
                entity.Property(cm => cm.ModelName).HasMaxLength(100).IsRequired(false);
                entity.Property(cm => cm.ExecutionTimeMs).IsRequired(false);
                entity.Property(cm => cm.PromptTokens).IsRequired(false);
                entity.Property(cm => cm.CompletionTokens).IsRequired(false);
                entity.Property(cm => cm.Feedback).IsRequired(false);

                // FK → ChatSessions (CASCADE)
                entity.HasOne(cm => cm.Session)
                    .WithMany(cs => cs.Messages)
                    .HasForeignKey(cm => cm.SessionID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cm => cm.SessionID).HasDatabaseName("IX_ChatMessages_SessionID");
            });

            // ============================================================
            // BẢNG TeacherChunkSettings
            // ============================================================
            modelBuilder.Entity<TeacherChunkSetting>(entity =>
            {
                entity.ToTable("TeacherChunkSettings");
                entity.HasKey(t => t.TeacherId);
                entity.Property(t => t.Strategy).IsRequired().HasMaxLength(50).HasDefaultValue("Recursive");
                entity.Property(t => t.ChunkSize).HasDefaultValue(800);
                entity.Property(t => t.ChunkOverlap).HasDefaultValue(100);
                entity.Property(t => t.MinChunkLength).HasDefaultValue(50);
                entity.Property(t => t.UpdatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(t => t.Teacher)
                    .WithOne()
                    .HasForeignKey<TeacherChunkSetting>(t => t.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // BẢNG DocumentConflicts
            // ============================================================
            modelBuilder.Entity<DocumentConflict>(entity =>
            {
                entity.ToTable("DocumentConflicts");
                entity.HasKey(dc => dc.ConflictID);
                entity.Property(dc => dc.ConflictID).UseIdentityColumn();
                entity.Property(dc => dc.Description).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(dc => dc.DetectedAt).HasDefaultValueSql("GETDATE()");

                // FK → Documents (DocumentID)
                entity.HasOne(dc => dc.Document)
                    .WithMany()
                    .HasForeignKey(dc => dc.DocumentID)
                    .OnDelete(DeleteBehavior.Restrict);

                // FK → Documents (ConflictingDocumentID)
                entity.HasOne(dc => dc.ConflictingDocument)
                    .WithMany()
                    .HasForeignKey(dc => dc.ConflictingDocumentID)
                    .OnDelete(DeleteBehavior.Restrict);

                // FK → DocumentChunks (ChunkID)
                entity.HasOne(dc => dc.Chunk)
                    .WithMany()
                    .HasForeignKey(dc => dc.ChunkID)
                    .OnDelete(DeleteBehavior.Restrict);

                // FK → DocumentChunks (ConflictingChunkID)
                entity.HasOne(dc => dc.ConflictingChunk)
                    .WithMany()
                    .HasForeignKey(dc => dc.ConflictingChunkID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(dc => dc.DocumentID).HasDatabaseName("IX_DocumentConflicts_DocumentID");
                entity.HasIndex(dc => dc.ConflictingDocumentID).HasDatabaseName("IX_DocumentConflicts_ConflictingDocumentID");
            });
        }
    }
}
