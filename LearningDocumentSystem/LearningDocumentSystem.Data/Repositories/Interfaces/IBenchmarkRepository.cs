using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IBenchmarkRepository
    {
        // Index Logs
        Task AddIndexLogAsync(IndexBenchmarkLog log);
        Task<IEnumerable<IndexBenchmarkLog>> GetIndexLogsAsync(int take = 100);

        // Query Logs
        Task AddQueryLogAsync(QueryBenchmarkLog log);
        Task<IEnumerable<QueryBenchmarkLog>> GetQueryLogsAsync(int take = 100);
        Task<bool> UpdateQueryRatingAsync(int logId, int rating);

        // Aggregated Stats for Dashboard
        Task<int> GetTotalQueryLogsAsync();
        Task<int> GetTotalIndexLogsAsync();
        Task<double> GetAvgRetrievalTimeMsAsync();
        Task<double> GetAvgGenerationTimeMsAsync();
        Task<(int ThumbsUp, int ThumbsDown, int NotRated)> GetRatingCountsAsync();

        // Per-model aggregations
        Task<IEnumerable<(string Model, double AvgMs)>> GetAvgRetrievalByEmbeddingModelAsync();
        Task<IEnumerable<(string LLM, double AvgMs)>> GetAvgGenerationByLLMAsync();
        Task<IEnumerable<(string LLM, double Rate)>> GetSatisfactionRateByLLMAsync();

        // Chunking strategy aggregations
        Task<IEnumerable<(string Strategy, double AvgCount, double AvgSize, double AvgProcessingMs)>> GetAvgChunkStatsByStrategyAsync();
        Task<IEnumerable<(string StrategyModel, double AvgProcessingMs)>> GetAvgProcessingByStrategyModelAsync();
    }
}
