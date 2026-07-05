using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class BenchmarkRepository : IBenchmarkRepository
    {
        private readonly AppDbContext _context;

        public BenchmarkRepository(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // INDEX LOGS
        // ============================================================

        public async Task AddIndexLogAsync(IndexBenchmarkLog log)
        {
            await _context.IndexBenchmarkLogs.AddAsync(log);
        }

        public async Task<IEnumerable<IndexBenchmarkLog>> GetIndexLogsAsync(int take = 100)
        {
            return await _context.IndexBenchmarkLogs
                .Include(l => l.Document)
                .OrderByDescending(l => l.ExecutedAt)
                .Take(take)
                .ToListAsync();
        }

        // ============================================================
        // QUERY LOGS
        // ============================================================

        public async Task AddQueryLogAsync(QueryBenchmarkLog log)
        {
            await _context.QueryBenchmarkLogs.AddAsync(log);
        }

        public async Task<IEnumerable<QueryBenchmarkLog>> GetQueryLogsAsync(int take = 100)
        {
            return await _context.QueryBenchmarkLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> UpdateQueryRatingAsync(int logId, int rating)
        {
            var log = await _context.QueryBenchmarkLogs.FindAsync(logId);
            if (log == null) return false;
            log.UserRating = rating;
            return true;
        }

        // ============================================================
        // AGGREGATE STATS
        // ============================================================

        public async Task<int> GetTotalQueryLogsAsync()
            => await _context.QueryBenchmarkLogs.CountAsync();

        public async Task<int> GetTotalIndexLogsAsync()
            => await _context.IndexBenchmarkLogs.CountAsync();

        public async Task<double> GetAvgRetrievalTimeMsAsync()
        {
            if (!await _context.QueryBenchmarkLogs.AnyAsync()) return 0;
            return await _context.QueryBenchmarkLogs.AverageAsync(l => l.RetrievalTimeMs);
        }

        public async Task<double> GetAvgGenerationTimeMsAsync()
        {
            if (!await _context.QueryBenchmarkLogs.AnyAsync()) return 0;
            return await _context.QueryBenchmarkLogs.AverageAsync(l => l.GenerationTimeMs);
        }

        public async Task<(int ThumbsUp, int ThumbsDown, int NotRated)> GetRatingCountsAsync()
        {
            var thumbsUp = await _context.QueryBenchmarkLogs.CountAsync(l => l.UserRating == 1);
            var thumbsDown = await _context.QueryBenchmarkLogs.CountAsync(l => l.UserRating == -1);
            var notRated = await _context.QueryBenchmarkLogs.CountAsync(l => l.UserRating == 0);
            return (thumbsUp, thumbsDown, notRated);
        }

        // ============================================================
        // PER-MODEL AGGREGATIONS
        // ============================================================

        public async Task<IEnumerable<(string Model, double AvgMs)>> GetAvgRetrievalByEmbeddingModelAsync()
        {
            var result = await _context.QueryBenchmarkLogs
                .GroupBy(l => l.EmbeddingModel)
                .Select(g => new { Model = g.Key, AvgMs = g.Average(l => l.RetrievalTimeMs) })
                .ToListAsync();
            return result.Select(r => (r.Model, r.AvgMs));
        }

        public async Task<IEnumerable<(string LLM, double AvgMs)>> GetAvgGenerationByLLMAsync()
        {
            var result = await _context.QueryBenchmarkLogs
                .GroupBy(l => l.LLMModel)
                .Select(g => new { LLM = g.Key, AvgMs = g.Average(l => l.GenerationTimeMs) })
                .ToListAsync();
            return result.Select(r => (r.LLM, r.AvgMs));
        }

        public async Task<IEnumerable<(string LLM, double Rate)>> GetSatisfactionRateByLLMAsync()
        {
            var groups = await _context.QueryBenchmarkLogs
                .GroupBy(l => l.LLMModel)
                .Select(g => new
                {
                    LLM = g.Key,
                    Total = g.Count(l => l.UserRating != 0),
                    Positive = g.Count(l => l.UserRating == 1)
                })
                .ToListAsync();

            return groups.Select(g => (
                g.LLM,
                Rate: g.Total > 0 ? Math.Round((double)g.Positive / g.Total * 100, 1) : 0.0
            ));
        }

        // ============================================================
        // CHUNKING STRATEGY AGGREGATIONS
        // ============================================================

        public async Task<IEnumerable<(string Strategy, double AvgCount, double AvgSize, double AvgProcessingMs)>> GetAvgChunkStatsByStrategyAsync()
        {
            var result = await _context.IndexBenchmarkLogs
                .GroupBy(l => l.ChunkingStrategy)
                .Select(g => new
                {
                    Strategy = g.Key,
                    AvgCount = g.Average(l => l.TotalChunksGenerated),
                    AvgSize = g.Average(l => l.AverageChunkSize),
                    AvgProcessingMs = g.Average(l => l.ProcessingTimeMs)
                })
                .ToListAsync();
            return result.Select(r => (r.Strategy, r.AvgCount, r.AvgSize, r.AvgProcessingMs));
        }

        public async Task<IEnumerable<(string StrategyModel, double AvgProcessingMs)>> GetAvgProcessingByStrategyModelAsync()
        {
            var result = await _context.IndexBenchmarkLogs
                .GroupBy(l => new { l.ChunkingStrategy, l.EmbeddingModel })
                .Select(g => new
                {
                    Label = g.Key.ChunkingStrategy + " × " + g.Key.EmbeddingModel,
                    AvgMs = g.Average(l => l.ProcessingTimeMs)
                })
                .ToListAsync();
            return result.Select(r => (r.Label, r.AvgMs));
        }
    }
}
