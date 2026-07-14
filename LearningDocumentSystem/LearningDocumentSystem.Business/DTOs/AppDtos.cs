using System.ComponentModel.DataAnnotations;

namespace LearningDocumentSystem.Business.DTOs
{
    // ================================================================
    // AUTH DTOs
    // ================================================================
    public class LoginDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class UserDto
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool CanUpload { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class RoleDto
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class AllowedEmailDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ================================================================
    // SUBJECT DTOs
    // ================================================================
    public class SubjectDto
    {
        public int SubjectID { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ChapterCount { get; set; }
        public int DocumentCount { get; set; }
        public int? SubjectLeaderID { get; set; }
        public string? SubjectLeaderName { get; set; }
    }

    public class CreateSubjectDto
    {
        [Required(ErrorMessage = "Tên môn học không được để trống")]
        [MaxLength(255)]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã học phần không được để trống")]
        [MaxLength(50)]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã học phần chỉ gồm chữ hoa và số")]
        public string SubjectCode { get; set; } = string.Empty;

        public int? SubjectLeaderID { get; set; }
    }

    public class UpdateSubjectDto : CreateSubjectDto
    {
        public int SubjectID { get; set; }
    }

    // ================================================================
    // CHAPTER DTOs
    // ================================================================
    public class ChapterDto
    {
        public int ChapterID { get; set; }
        public int SubjectID { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int ChapterNumber { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int DocumentCount { get; set; }
    }

    public class CreateChapterDto
    {
        [Required]
        public int SubjectID { get; set; }

        [Required(ErrorMessage = "Số chương không được để trống")]
        [Range(1, 100, ErrorMessage = "Số chương từ 1-100")]
        public int ChapterNumber { get; set; }

        [Required(ErrorMessage = "Tên chương không được để trống")]
        [MaxLength(255)]
        public string ChapterName { get; set; } = string.Empty;
    }

    public class UpdateChapterDto : CreateChapterDto
    {
        public int ChapterID { get; set; }
    }

    // ================================================================
    // DOCUMENT DTOs
    // ================================================================
    public class DocumentDto
    {
        public int DocumentID { get; set; }
        public int ChapterID { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int ChapterNumber { get; set; }
        public int SubjectID { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public string IndexStatus { get; set; } = "Pending";
        public int UploadedBy { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime? IndexedAt { get; set; }
        public int ChunkCount { get; set; }
    }

    public class DocumentConflictDto
    {
        public int ConflictID { get; set; }
        public int DocumentID { get; set; }
        public int ConflictingDocumentID { get; set; }
        public string ConflictingDocumentTitle { get; set; } = string.Empty;
        public int ChunkID { get; set; }
        public int ConflictingChunkID { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
    }

    public class DocumentDetailDto : DocumentDto
    {
        public List<ChunkDto> Chunks { get; set; } = new();
        public List<DocumentConflictDto> Conflicts { get; set; } = new();
    }

    public class ChunkDto
    {
        public int ChunkID { get; set; }
        public int ChunkIndex { get; set; }
        public int? PageNumber { get; set; }
        public string ContentText { get; set; } = string.Empty;
        public bool HasEmbedding { get; set; }
    }

    public class ChatResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public List<ChatSourceDto> Sources { get; set; } = new();
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public double ExecutionTimeMs { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }

    public class ChatSourceDto
    {
        public int DocumentID { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public float SimilarityScore { get; set; }
        public string ContentSnippet { get; set; } = string.Empty;
    }

    // ================================================================
    // CHAT SESSION DTOs
    // ================================================================
    public class ChatSessionDto
    {
        public int SessionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
        public string? LastMessagePreview { get; set; }
    }

    public class ChatMessageDto
    {
        public int MessageID { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<ChatSourceDto> Sources { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string? ProviderName { get; set; }
        public string? ModelName { get; set; }
        public double? ExecutionTimeMs { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? Feedback { get; set; }
    }

    public class CreateChatSessionDto
    {
        public string? Title { get; set; }
        public int? SubjectId { get; set; }
    }

    public class MonthlyUploadDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label => $"{Month:D2}/{Year}";
        public int Count { get; set; }
    }

    public class DashboardDto
    {
        public int TotalDocuments { get; set; }
        public int TotalChunks { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalUsers { get; set; }
        public int IndexedDocuments { get; set; }
        public int PendingDocuments { get; set; }
        public int ProcessingDocuments { get; set; }
        public int FailedDocuments { get; set; }
        public List<DocumentDto> RecentDocuments { get; set; } = new();
        public List<MonthlyUploadDto> MonthlyUploads { get; set; } = new();
        public List<MonthlyUploadDto> MonthlyRegistrations { get; set; } = new();
    }

    // ================================================================
    // CHUNK SETTINGS DTOs
    // ================================================================
    public class ChunkSettingsDto
    {
        public string Strategy { get; set; } = "Recursive";
        public int ChunkSize { get; set; } = 800;
        public int ChunkOverlap { get; set; } = 100;
        public int MinChunkLength { get; set; } = 50;
        public DateTime? UpdatedAt { get; set; }
    }

    // ================================================================
    // PACKAGE DTOs
    // ================================================================
    public class PackagePlanDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? DailyMessageLimit { get; set; }
        public List<string> AllowedProviders { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PackageStatusDto
    {
        public string CurrentPlan { get; set; } = "Free";
        public int UsedToday { get; set; }
        public int? DailyMessageLimit { get; set; }
        public int? RemainingToday { get; set; }
        public List<string> AllowedProviders { get; set; } = new();
    }

    public class ChatAccessDto
    {
        public bool IsAllowed { get; set; }
        public string Message { get; set; } = string.Empty;
        public PackageStatusDto Status { get; set; } = new();
    }

    public class VnpayPaymentResultDto
    {
        public bool IsValid { get; set; }
        public bool IsSuccess { get; set; }
        public int UserId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
