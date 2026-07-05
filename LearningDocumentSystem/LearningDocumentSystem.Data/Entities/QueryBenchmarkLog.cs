using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    /// <summary>
    /// Lưu vết từng lượt truy vấn RAG của người dùng.
    /// Mỗi câu hỏi được gửi đến ChatService → 1 dòng log.
    /// </summary>
    public class QueryBenchmarkLog
    {
        [Key]
        [Column("LogID")]
        public int LogID { get; set; }

        /// <summary>Nội dung câu hỏi của người dùng.</summary>
        [Required]
        [MaxLength(2000)]
        public string QueryText { get; set; } = string.Empty;

        /// <summary>Mô hình LLM sinh câu trả lời: "Gemini", "GPT-4o-mini", "Llama-3-8b", ...</summary>
        [Required]
        [MaxLength(100)]
        public string LLMModel { get; set; } = string.Empty;

        /// <summary>Mô hình nhúng được dùng để truy xuất: "TF-IDF", "multilingual-e5-base", ...</summary>
        [Required]
        [MaxLength(100)]
        public string EmbeddingModel { get; set; } = string.Empty;

        /// <summary>Thời gian sinh vector + tìm kiếm semantic trong DB (milliseconds).</summary>
        public double RetrievalTimeMs { get; set; }

        /// <summary>Thời gian LLM phản hồi (milliseconds).</summary>
        public double GenerationTimeMs { get; set; }

        /// <summary>Điểm Cosine Similarity của chunk được xếp hạng cao nhất (0.0 – 1.0).</summary>
        public double Top1CosineSimilarity { get; set; }

        /// <summary>Số nguồn (chunks) được chọn để xây dựng context.</summary>
        public int SelectedSourcesCount { get; set; }

        /// <summary>Đánh giá của người dùng: 1 = Thumbs Up, -1 = Thumbs Down, 0 = Chưa đánh giá.</summary>
        public int UserRating { get; set; } = 0;

        /// <summary>Thời điểm tạo log (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
