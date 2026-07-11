using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class ChatSessionRepository : GenericRepository<ChatSession>, IChatSessionRepository
    {
        public ChatSessionRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<ChatSession>> GetSessionsByUserAsync(int userId)
            => await _context.ChatSessions
                .Include(cs => cs.Messages)
                .Where(cs => cs.UserID == userId)
                .OrderByDescending(cs => cs.UpdatedAt)
                .ToListAsync();

        public async Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId, int userId)
            => await _context.ChatSessions
                .Include(cs => cs.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(cs => cs.SessionID == sessionId && cs.UserID == userId);

        public async Task UpdateTitleAsync(int sessionId, string newTitle)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null)
            {
                session.Title = newTitle;
                session.UpdatedAt = DateTime.UtcNow;
                _context.ChatSessions.Update(session);
            }
        }

        public async Task TouchUpdatedAtAsync(int sessionId)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null)
            {
                session.UpdatedAt = DateTime.UtcNow;
                _context.ChatSessions.Update(session);
            }
        }

        public async Task AddMessageAsync(ChatMessage message)
            => await _context.ChatMessages.AddAsync(message);

        public async Task<ChatMessage?> GetMessageAsync(int messageId)
            => await _context.ChatMessages.FindAsync(messageId);

        public async Task UpdateMessageFeedbackAsync(int messageId, int feedback)
        {
            var msg = await _context.ChatMessages.FindAsync(messageId);
            if (msg != null)
            {
                msg.Feedback = feedback;
                _context.ChatMessages.Update(msg);
            }
        }

        public async Task<IEnumerable<ChatMessage>> GetAllMessagesWithUserAsync()
            => await _context.ChatMessages
                .Include(m => m.Session)
                    .ThenInclude(s => s.User)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

        public async Task<int> GetTotalSessionCountAsync()
            => await _context.ChatSessions.CountAsync();
    }
}
