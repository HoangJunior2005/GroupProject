namespace LearningDocumentSystem.Business.DTOs
{
    // ============================================================
    // DTOs cho Benchmark Dashboard
    // ============================================================

    /// <summary>DTO hiển thị log index một tài liệu.</summary>
    public class IndexBenchmarkLogDto
    {
        public int LogID { get; set; }
        public int? DocumentID { get; set; }
        public string? DocumentTitle { get; set; }
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public int TotalChunksGenerated { get; set; }
        public double ProcessingTimeMs { get; set; }
        public double AverageChunkSize { get; set; }
        public DateTime ExecutedAt { get; set; }
    }

    /// <summary>DTO hiển thị log một lượt truy vấn RAG.</summary>
    public class QueryBenchmarkLogDto
    {
        public int LogID { get; set; }
        public string QueryText { get; set; } = string.Empty;
        public string LLMModel { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public double RetrievalTimeMs { get; set; }
        public double GenerationTimeMs { get; set; }
        public double TotalLatencyMs => RetrievalTimeMs + GenerationTimeMs;
        public double Top1CosineSimilarity { get; set; }
        public int SelectedSourcesCount { get; set; }
        public int UserRating { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>KPI Stats tổng hợp hiển thị trên Dashboard.</summary>
    public class BenchmarkStatsDto
    {
        // KPI Cards
        public int TotalQueryLogs { get; set; }
        public int TotalIndexLogs { get; set; }
        public double AvgRetrievalTimeMs { get; set; }
        public double AvgGenerationTimeMs { get; set; }
        public double HelfulnessRatePercent { get; set; }      // % ThumbsUp trong tổng rated
        public double AvgTop1CosineSimilarity { get; set; }
        public int TotalThumbsUp { get; set; }
        public int TotalThumbsDown { get; set; }
        public int TotalNotRated { get; set; }

        // Chart data
        public List<string> EmbeddingModelLabels { get; set; } = new();
        public List<double> AvgRetrievalByModel { get; set; } = new();

        public List<string> LLMModelLabels { get; set; } = new();
        public List<double> AvgGenerationByLLM { get; set; } = new();

        public List<string> ChunkingStrategyLabels { get; set; } = new();
        public List<double> AvgChunkCountByStrategy { get; set; } = new();
        public List<double> AvgChunkSizeByStrategy { get; set; } = new();
        public List<double> AvgProcessingTimeByStrategy { get; set; } = new();

        // Processing time by (strategy × model) for grouped chart
        public List<string> StrategyModelLabels { get; set; } = new();
        public List<double> ProcessingTimeStrategyModel { get; set; } = new();

        // Satisfaction by LLM
        public List<string> SatisfactionLabels { get; set; } = new();   // LLM names
        public List<double> SatisfactionRates { get; set; } = new();    // % thumbs up

        // Recent logs
        public List<QueryBenchmarkLogDto> RecentQueryLogs { get; set; } = new();
        public List<IndexBenchmarkLogDto> RecentIndexLogs { get; set; } = new();
    }

    // ============================================================
    // DTOs cho Playground (thử nghiệm trực tiếp)
    // ============================================================

    /// <summary>Request gửi lên khi chạy playground benchmark.</summary>
    public class PlaygroundRequestDto
    {
        public string QueryText { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public int? ChapterId { get; set; }
        /// <summary>Danh sách model muốn so sánh. Mặc định chạy tất cả.</summary>
        public List<string> Models { get; set; } = new() { "TF-IDF", "multilingual-e5-base", "PhoBERT-base", "bge-m3" };
    }

    /// <summary>Kết quả của một model trong Playground.</summary>
    public class PlaygroundModelResultDto
    {
        public string ModelName { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;    // "Native" | "Stub" | "External"
        public int Dimensions { get; set; }
        public double EmbeddingTimeMs { get; set; }
        public double RetrievalTimeMs { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? UnavailableReason { get; set; }
        public List<PlaygroundChunkResultDto> TopChunks { get; set; } = new();
    }

    /// <summary>Một chunk kết quả trong Playground.</summary>
    public class PlaygroundChunkResultDto
    {
        public int ChunkId { get; set; }
        public int DocumentID { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public double CosineSimilarity { get; set; }
        public string ContentSnippet { get; set; } = string.Empty;
    }

    /// <summary>Response tổng hợp toàn bộ kết quả Playground.</summary>
    public class PlaygroundResultDto
    {
        public string QueryText { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public List<PlaygroundModelResultDto> ModelResults { get; set; } = new();
    }
}
