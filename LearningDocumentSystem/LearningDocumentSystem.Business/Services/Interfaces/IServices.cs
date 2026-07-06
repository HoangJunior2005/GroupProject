using LearningDocumentSystem.Business.DTOs;
using Microsoft.AspNetCore.Http;

namespace LearningDocumentSystem.Business.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto?> LoginAsync(string username, string password);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<UserDto> RegisterAsync(string email, string password);
    }

    public interface IAdminUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task UpdateUserRolesAsync(int userId, IEnumerable<int> roleIds, bool canUpload);
        Task<int> ImportAllowedEmailsAsync(IFormFile file);
        Task<IEnumerable<AllowedEmailDto>> GetAllowedEmailsAsync();
        Task DeleteAllowedEmailAsync(int id);
        Task CreateTeacherAccountAsync(string email, string fullName, string password);
        Task DeleteUserAsync(int userId);
        Task UpdateUploadPermissionAsync(int userId, bool canUpload);
    }

    public interface ISubjectService
    {
        Task<IEnumerable<SubjectDto>> GetAllAsync();
        Task<SubjectDto?> GetByIdAsync(int id);
        Task<SubjectDto?> GetWithChaptersAsync(int id);
        Task<SubjectDto> CreateAsync(CreateSubjectDto dto);
        Task<SubjectDto> UpdateAsync(UpdateSubjectDto dto);
        Task DeleteAsync(int id);
        Task AssignLeaderAsync(int subjectId, int? leaderId);
    }

    public interface IChapterService
    {
        Task<IEnumerable<ChapterDto>> GetAllAsync();
        Task<IEnumerable<ChapterDto>> GetBySubjectAsync(int subjectId);
        Task<ChapterDto?> GetByIdAsync(int id);
        Task<ChapterDto> CreateAsync(CreateChapterDto dto);
        Task<ChapterDto> UpdateAsync(UpdateChapterDto dto);
        Task DeleteAsync(int id);
    }

    public interface IDocumentService
    {
        Task<(IEnumerable<DocumentDto> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, string? status, int? teacherId, int page, int pageSize);
        Task<DocumentDetailDto?> GetDetailAsync(int id);
        Task<DocumentDto> UploadAsync(IFormFile file, int chapterId, string title, int uploadedByUserId);
        Task DeleteAsync(int id);
        Task<DashboardDto> GetDashboardAsync();
    }

    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string uploadFolder);
        void DeleteFile(string storagePath, string uploadFolder);
    }

    public interface IChunkingService
    {
        Task<List<(string Content, int PageNumber)>> ExtractChunksAsync(string filePath, string fileType);
    }

    public interface IEmbeddingService
    {
        /// <summary>
        /// Sinh vector embedding cho văn bản bằng thuật toán Feature Hashing + TF-IDF weighting.
        /// Vector có số chiều cố định, chuẩn hóa thành unit vector để tính cosine similarity.
        /// </summary>
        Task<string> GenerateEmbeddingAsync(string text);
    }

    public interface IChatService
    {
        Task<ChatResponseDto> AskQuestionAsync(string question, int? subjectId = null, int? chapterId = null, string? modelProvider = null);

        // Session management
        Task<ChatSessionDto> CreateSessionAsync(int userId, string? title = null, int? subjectId = null);
        Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(int userId);
        Task<IEnumerable<ChatMessageDto>> GetSessionMessagesAsync(int sessionId, int userId);
        Task<int> SaveMessagesAsync(int sessionId, string userContent, string assistantContent, List<ChatSourceDto>? sources, string? providerName = null, string? modelName = null, double? executionTimeMs = null, int? promptTokens = null, int? completionTokens = null);
        Task DeleteSessionAsync(int sessionId, int userId);
        Task UpdateSessionTitleAsync(int sessionId, int userId, string title);
        Task UpdateSessionSubjectAsync(int sessionId, int userId, int? subjectId);
        Task UpdateMessageFeedbackAsync(int messageId, int userId, int feedback);
    }
}
