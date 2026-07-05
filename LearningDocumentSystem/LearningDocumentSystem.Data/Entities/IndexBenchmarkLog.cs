using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    /// <summary>
    /// Lưu vết hiệu năng quá trình phân mảnh + sinh embedding khi index tài liệu.
    /// Mỗi lần index 1 tài liệu với 1 cấu hình (chunking strategy × embedding model) → 1 dòng log.
    /// </summary>
    public class IndexBenchmarkLog
    {
        [Key]
        [Column("LogID")]
        public int LogID { get; set; }

        /// <summary>ID tài liệu được index. Nullable để giữ log dù document bị xóa.</summary>
        public int? DocumentID { get; set; }

        /// <summary>Chiến lược phân mảnh: "Paragraph", "FixedSize", "Recursive"</summary>
        [Required]
        [MaxLength(50)]
        public string ChunkingStrategy { get; set; } = string.Empty;

        /// <summary>Mô hình nhúng: "TF-IDF", "multilingual-e5-base", "text-embedding-3-small", "PhoBERT-base", "bge-m3"</summary>
        [Required]
        [MaxLength(100)]
        public string EmbeddingModel { get; set; } = string.Empty;

        /// <summary>Tổng số chunks được tạo ra cho tài liệu này.</summary>
        public int TotalChunksGenerated { get; set; }

        /// <summary>Tổng thời gian xử lý (phân mảnh + sinh embedding) tính bằng milliseconds.</summary>
        public double ProcessingTimeMs { get; set; }

        /// <summary>Kích thước trung bình của mỗi chunk (tính bằng ký tự).</summary>
        public double AverageChunkSize { get; set; }

        /// <summary>Thời điểm thực hiện benchmark (UTC).</summary>
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        // Navigation property (nullable — document có thể bị xóa)
        [ForeignKey(nameof(DocumentID))]
        public virtual Document? Document { get; set; }
    }
}
