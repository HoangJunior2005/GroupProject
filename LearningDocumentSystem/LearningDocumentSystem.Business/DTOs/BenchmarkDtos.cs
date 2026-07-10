namespace LearningDocumentSystem.Business.DTOs
{
    // ================================================================
    // BENCHMARK DASHBOARD DTOs
    // ================================================================

    /// <summary>
    /// Tổng hợp toàn bộ dữ liệu cho trang Benchmark Dashboard.
    /// </summary>
    public class BenchmarkDashboardDto
    {
        // --- KPI Cards ---
        public int TotalQueries { get; set; }
        public double AvgRetrievalTimeMs { get; set; }
        public double AvgGenerationTimeMs { get; set; }
        public double HelpfulnessRate { get; set; }
        public double AvgChunksPerDocument { get; set; }
        public double AvgChunkSizeChars { get; set; }

        // --- Chart Data ---
        public List<EmbeddingLatencyDto> EmbeddingLatencies { get; set; } = new();
        public List<LlmLatencyDto> LlmLatencies { get; set; } = new();
        public List<SatisfactionRateDto> SatisfactionRates { get; set; } = new();
        public List<ChunkingBenchmarkDto> ChunkingBenchmarks { get; set; } = new();
    }

    /// <summary>
    /// Thời gian truy xuất trung bình của mỗi mô hình nhúng (ms).
    /// </summary>
    public class EmbeddingLatencyDto
    {
        public string ModelName { get; set; } = string.Empty;
        public double AvgLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
    }

    /// <summary>
    /// Thời gian sinh câu trả lời trung bình của mỗi mô hình LLM (ms).
    /// </summary>
    public class LlmLatencyDto
    {
        public string ModelName { get; set; } = string.Empty;
        public double AvgLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
    }

    /// <summary>
    /// Tỷ lệ hài lòng (Thumbs Up %) theo tổ hợp Strategy + Embedding + LLM.
    /// </summary>
    public class SatisfactionRateDto
    {
        public string Label { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public string LlmModel { get; set; } = string.Empty;
        public double ThumbsUpPercent { get; set; }
        public double ThumbsDownPercent { get; set; }
        public int TotalVotes { get; set; }
    }

    /// <summary>
    /// Benchmark cho mỗi chiến lược chunking: avg chunk count & avg chunk size.
    /// </summary>
    public class ChunkingBenchmarkDto
    {
        public string Strategy { get; set; } = string.Empty;
        public double AvgChunkCount { get; set; }
        public double AvgChunkSize { get; set; }
        public int TotalDocuments { get; set; }
    }

    // ================================================================
    // PLAYGROUND DTOs
    // ================================================================

    /// <summary>
    /// Kết quả thử nghiệm truy xuất từ một mô hình nhúng cụ thể.
    /// </summary>
    public class PlaygroundResultDto
    {
        public string ModelName { get; set; } = string.Empty;
        public double EmbeddingTimeMs { get; set; }
        public double SearchTimeMs { get; set; }
        public double TotalTimeMs => EmbeddingTimeMs + SearchTimeMs;
        public List<PlaygroundChunkResultDto> TopChunks { get; set; } = new();
    }

    /// <summary>
    /// Một chunk được tìm thấy bởi playground, kèm cosine score.
    /// </summary>
    public class PlaygroundChunkResultDto
    {
        public int ChunkID { get; set; }
        public int DocumentID { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public string ContentSnippet { get; set; } = string.Empty;
        public double CosineScore { get; set; }
    }
}
