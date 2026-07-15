using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class UserRole
    {
        public int UserID { get; set; }
        public int RoleID { get; set; }

        /// <summary>
        /// Ngày hết hạn gói trả phí. Null = không giới hạn (gói Free hoặc các role hệ thống).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
    }
}
