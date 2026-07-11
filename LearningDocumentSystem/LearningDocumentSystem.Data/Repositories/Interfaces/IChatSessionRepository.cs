using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IChatSessionRepository : IGenericRepository<ChatSession>
    {
        /// <summary>Lấy danh sách phiên của người dùng, sắp xếp mới nhất trước.</summary>
        Task<IEnumerable<ChatSession>> GetSessionsByUserAsync(int userId);

        /// <summary>Lấy phiên kèm toàn bộ messages (chỉ của đúng user).</summary>
        Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId, int userId);

        /// <summary>Cập nhật tiêu đề phiên.</summary>
        Task UpdateTitleAsync(int sessionId, string newTitle);

        /// <summary>Cập nhật UpdatedAt của phiên (sau mỗi tin nhắn mới).</summary>
        Task TouchUpdatedAtAsync(int sessionId);

        /// <summary>Lưu 1 tin nhắn vào phiên.</summary>
        Task AddMessageAsync(ChatMessage message);

        /// <summary>Lấy tin nhắn theo ID.</summary>
        Task<ChatMessage?> GetMessageAsync(int messageId);

        /// <summary>Cập nhật phản hồi benchmark (thumbs up/down) cho tin nhắn.</summary>
        Task UpdateMessageFeedbackAsync(int messageId, int feedback);

        /// <summary>Lấy toàn bộ ChatMessage kèm Session + User — dùng cho analytics.</summary>
        Task<IEnumerable<ChatMessage>> GetAllMessagesWithUserAsync();

        /// <summary>Lấy tổng số sessions.</summary>
        Task<int> GetTotalSessionCountAsync();
    }
}
