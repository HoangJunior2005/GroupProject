namespace LearningDocumentSystem.Business.DTOs
{
    // ================================================================
    // THỐNG KÊ HOẠT ĐỘNG HỌC TẬP — DTOs
    // ================================================================

    /// <summary>
    /// Tổng hợp toàn bộ dữ liệu cho trang Thống kê Hoạt động.
    /// </summary>
    public class BenchmarkDashboardDto
    {
        // --- KPI ---
        public int TotalActiveStudents { get; set; }
        public int TotalSessions { get; set; }
        public int TotalQueries { get; set; }
        public string MostUsedModel { get; set; } = "—";

        // --- Chart Data ---
        public List<ModelUsageDto> ModelUsage { get; set; } = new();
        public List<ModelRatingDto> ModelRatings { get; set; } = new();
        public List<MonthlyTokenDto> MonthlyTokens { get; set; } = new();
        public List<MonthlyLatencyDto> MonthlyLatency { get; set; } = new();
    }

    /// <summary>Tần suất sử dụng từng Model AI bởi sinh viên.</summary>
    public class ModelUsageDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int TotalQueries { get; set; }
        public int UniqueStudents { get; set; }
        public double UsagePercent { get; set; }
    }

    /// <summary>Đánh giá (thumbs up/down) của từng Model AI.</summary>
    public class ModelRatingDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public int ThumbsUp { get; set; }
        public int ThumbsDown { get; set; }
        public int TotalVotes { get; set; }
        public double RatingPercent { get; set; }
    }

    /// <summary>Lượng token tiêu thụ hàng tháng theo từng provider.</summary>
    public class MonthlyTokenDto
    {
        public string Month { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public int TotalPromptTokens { get; set; }
        public int TotalCompletionTokens { get; set; }
        public int TotalTokens => TotalPromptTokens + TotalCompletionTokens;
        public int QueryCount { get; set; }
    }

    /// <summary>Latency trung bình hàng tháng theo từng provider.</summary>
    public class MonthlyLatencyDto
    {
        public string Month { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public double AvgLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public int SampleCount { get; set; }
    }

    // ================================================================
    // PLAYGROUND DTOs (giữ lại cho backward compat — không dùng)
    // ================================================================
    public class PlaygroundResultDto
    {
        public string ModelName { get; set; } = string.Empty;
        public double EmbeddingTimeMs { get; set; }
        public double SearchTimeMs { get; set; }
        public double TotalTimeMs => EmbeddingTimeMs + SearchTimeMs;
        public List<PlaygroundChunkResultDto> TopChunks { get; set; } = new();
    }

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
