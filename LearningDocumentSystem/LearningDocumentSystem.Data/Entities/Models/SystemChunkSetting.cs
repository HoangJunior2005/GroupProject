using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    /// <summary>
    /// Lưu cấu hình chunking toàn hệ thống (do Admin thiết lập).
    /// </summary>
    public class SystemChunkSetting
    {
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// Chiến lược phân mảnh: "FixedSize" | "Paragraph" | "Recursive"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Strategy { get; set; } = "Recursive";

        /// <summary>Kích thước chunk tối đa (ký tự)</summary>
        public int ChunkSize { get; set; } = 800;

        /// <summary>Số ký tự trùng lặp giữa 2 chunk liên tiếp</summary>
        public int ChunkOverlap { get; set; } = 100;

        /// <summary>Độ dài tối thiểu để chunk được chấp nhận (ký tự)</summary>
        public int MinChunkLength { get; set; } = 50;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
