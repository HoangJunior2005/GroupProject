using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class BenchmarkService : IBenchmarkService
    {
        private readonly IUnitOfWork _uow;
        private readonly EmbeddingAdapterFactory _adapterFactory;
        private readonly ILogger<BenchmarkService> _logger;

        public BenchmarkService(
            IUnitOfWork uow,
            EmbeddingAdapterFactory adapterFactory,
            ILogger<BenchmarkService> logger)
        {
            _uow = uow;
            _adapterFactory = adapterFactory;
            _logger = logger;
        }

        // ============================================================
        // DASHBOARD STATS
        // ============================================================

        public async Task<BenchmarkStatsDto> GetDashboardStatsAsync()
        {
            var repo = _uow.BenchmarkLogs;
            var stats = new BenchmarkStatsDto
            {
                TotalQueryLogs = await repo.GetTotalQueryLogsAsync(),
                TotalIndexLogs = await repo.GetTotalIndexLogsAsync(),
                AvgRetrievalTimeMs = Math.Round(await repo.GetAvgRetrievalTimeMsAsync(), 2),
                AvgGenerationTimeMs = Math.Round(await repo.GetAvgGenerationTimeMsAsync(), 2),
            };

            // Rating
            var (up, down, notRated) = await repo.GetRatingCountsAsync();
            stats.TotalThumbsUp = up;
            stats.TotalThumbsDown = down;
            stats.TotalNotRated = notRated;
            int totalRated = up + down;
            stats.HelfulnessRatePercent = totalRated > 0
                ? Math.Round((double)up / totalRated * 100, 1)
                : 0;

            // Avg Retrieval by embedding model
            var byModel = (await repo.GetAvgRetrievalByEmbeddingModelAsync()).ToList();
            stats.EmbeddingModelLabels = byModel.Select(x => x.Model).ToList();
            stats.AvgRetrievalByModel = byModel.Select(x => Math.Round(x.AvgMs, 2)).ToList();

            // Avg Generation by LLM
            var byLlm = (await repo.GetAvgGenerationByLLMAsync()).ToList();
            stats.LLMModelLabels = byLlm.Select(x => x.LLM).ToList();
            stats.AvgGenerationByLLM = byLlm.Select(x => Math.Round(x.AvgMs, 2)).ToList();

            // Satisfaction by LLM
            var satisfaction = (await repo.GetSatisfactionRateByLLMAsync()).ToList();
            stats.SatisfactionLabels = satisfaction.Select(x => x.LLM).ToList();
            stats.SatisfactionRates = satisfaction.Select(x => x.Rate).ToList();

            // Chunking strategy stats
            var chunkStats = (await repo.GetAvgChunkStatsByStrategyAsync()).ToList();
            stats.ChunkingStrategyLabels = chunkStats.Select(x => x.Strategy).ToList();
            stats.AvgChunkCountByStrategy = chunkStats.Select(x => Math.Round(x.AvgCount, 1)).ToList();
            stats.AvgChunkSizeByStrategy = chunkStats.Select(x => Math.Round(x.AvgSize, 1)).ToList();
            stats.AvgProcessingTimeByStrategy = chunkStats.Select(x => Math.Round(x.AvgProcessingMs, 2)).ToList();

            // Processing time by strategy × model
            var strategyModel = (await repo.GetAvgProcessingByStrategyModelAsync()).ToList();
            stats.StrategyModelLabels = strategyModel.Select(x => x.StrategyModel).ToList();
            stats.ProcessingTimeStrategyModel = strategyModel.Select(x => Math.Round(x.AvgProcessingMs, 2)).ToList();

            // Recent logs
            var recentQuery = (await repo.GetQueryLogsAsync(20)).ToList();
            stats.RecentQueryLogs = recentQuery.Select(q => new QueryBenchmarkLogDto
            {
                LogID = q.LogID,
                QueryText = q.QueryText.Length > 100 ? q.QueryText[..100] + "..." : q.QueryText,
                LLMModel = q.LLMModel,
                EmbeddingModel = q.EmbeddingModel,
                RetrievalTimeMs = q.RetrievalTimeMs,
                GenerationTimeMs = q.GenerationTimeMs,
                Top1CosineSimilarity = q.Top1CosineSimilarity,
                SelectedSourcesCount = q.SelectedSourcesCount,
                UserRating = q.UserRating,
                CreatedAt = q.CreatedAt
            }).ToList();

            var recentIndex = (await repo.GetIndexLogsAsync(10)).ToList();
            stats.RecentIndexLogs = recentIndex.Select(i => new IndexBenchmarkLogDto
            {
                LogID = i.LogID,
                DocumentID = i.DocumentID,
                DocumentTitle = i.Document?.Title,
                ChunkingStrategy = i.ChunkingStrategy,
                EmbeddingModel = i.EmbeddingModel,
                TotalChunksGenerated = i.TotalChunksGenerated,
                ProcessingTimeMs = i.ProcessingTimeMs,
                AverageChunkSize = i.AverageChunkSize,
                ExecutedAt = i.ExecutedAt
            }).ToList();

            return stats;
        }

        // ============================================================
        // LOGGING
        // ============================================================

        public async Task LogIndexBenchmarkAsync(IndexBenchmarkLogDto dto)
        {
            var entity = new IndexBenchmarkLog
            {
                DocumentID = dto.DocumentID,
                ChunkingStrategy = dto.ChunkingStrategy,
                EmbeddingModel = dto.EmbeddingModel,
                TotalChunksGenerated = dto.TotalChunksGenerated,
                ProcessingTimeMs = dto.ProcessingTimeMs,
                AverageChunkSize = dto.AverageChunkSize,
                ExecutedAt = DateTime.UtcNow
            };
            await _uow.BenchmarkLogs.AddIndexLogAsync(entity);
            await _uow.SaveChangesAsync();
        }

        public async Task LogQueryBenchmarkAsync(QueryBenchmarkLogDto dto)
        {
            var entity = new QueryBenchmarkLog
            {
                QueryText = dto.QueryText.Length > 2000 ? dto.QueryText[..2000] : dto.QueryText,
                LLMModel = dto.LLMModel,
                EmbeddingModel = dto.EmbeddingModel,
                RetrievalTimeMs = dto.RetrievalTimeMs,
                GenerationTimeMs = dto.GenerationTimeMs,
                Top1CosineSimilarity = dto.Top1CosineSimilarity,
                SelectedSourcesCount = dto.SelectedSourcesCount,
                UserRating = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.BenchmarkLogs.AddQueryLogAsync(entity);
            await _uow.SaveChangesAsync();
        }

        public async Task<bool> UpdateQueryRatingAsync(int logId, int rating)
        {
            var result = await _uow.BenchmarkLogs.UpdateQueryRatingAsync(logId, rating);
            if (result) await _uow.SaveChangesAsync();
            return result;
        }

        // ============================================================
        // PLAYGROUND — chạy multi-model retrieval đồng thời
        // ============================================================

        public async Task<PlaygroundResultDto> RunPlaygroundAsync(PlaygroundRequestDto request)
        {
            var result = new PlaygroundResultDto
            {
                QueryText = request.QueryText,
                ExecutedAt = DateTime.UtcNow
            };

            // Lấy tất cả chunks trong DB (filter theo subject/chapter nếu có)
            var allChunks = (await _uow.DocumentChunks.GetChunksForRAGAsync(request.SubjectId, request.ChapterId))
                .ToList();

            // Tất cả models cần chạy
            var models = request.Models.Any()
                ? request.Models
                : new List<string> { "TF-IDF", "multilingual-e5-base", "PhoBERT-base", "bge-m3" };

            // Chạy song song
            var tasks = models.Select(modelName => RunSingleModelAsync(modelName, request.QueryText, allChunks));
            var modelResults = await Task.WhenAll(tasks);
            result.ModelResults = modelResults.ToList();

            return result;
        }

        private async Task<PlaygroundModelResultDto> RunSingleModelAsync(
            string modelName,
            string queryText,
            List<LearningDocumentSystem.Entities.Models.DocumentChunk> allChunks)
        {
            var adapter = _adapterFactory.GetAdapter(modelName);
            var modelResult = new PlaygroundModelResultDto
            {
                ModelName = adapter.ModelName,
                ModelType = adapter.ModelType,
                Dimensions = adapter.Dimensions,
                IsAvailable = adapter.IsAvailable,
                UnavailableReason = adapter.UnavailableReason
            };

            if (!adapter.IsAvailable)
                return modelResult;

            try
            {
                // Sinh embedding cho query
                var (queryVector, embeddingTimeMs) = await adapter.EmbedAsync(queryText);
                modelResult.EmbeddingTimeMs = Math.Round(embeddingTimeMs, 2);

                if (queryVector.Length == 0)
                {
                    modelResult.IsAvailable = false;
                    modelResult.UnavailableReason = "Không thể sinh embedding (service có thể đang offline).";
                    return modelResult;
                }

                // Tìm kiếm trong DB
                var retrievalSw = Stopwatch.StartNew();
                var scored = new List<(float Score, LearningDocumentSystem.Entities.Models.DocumentChunk Chunk)>();

                // Chỉ dùng chunks có embedding từ TF-IDF (512 dim) cho TF-IDF adapter
                // Các adapter khác sẽ so sánh vector query (dim khác) với vector chunk TF-IDF
                // bằng normalized dot product để demo — trong thực tế cần re-index với model mới.
                foreach (var chunk in allChunks)
                {
                    if (chunk.Embedding == null || string.IsNullOrWhiteSpace(chunk.Embedding.VectorData))
                        continue;

                    try
                    {
                        var chunkVector = JsonSerializer.Deserialize<float[]>(chunk.Embedding.VectorData);
                        if (chunkVector == null || chunkVector.Length == 0) continue;

                        float score;
                        if (queryVector.Length == chunkVector.Length)
                        {
                            // Same dimension — dot product of unit vectors = cosine similarity
                            score = DotProduct(queryVector, chunkVector);
                        }
                        else
                        {
                            // Different dimensions: project to min dim for approximate comparison
                            int minDim = Math.Min(queryVector.Length, chunkVector.Length);
                            score = DotProductPartial(queryVector, chunkVector, minDim);
                        }
                        scored.Add((score, chunk));
                    }
                    catch { /* skip malformed vectors */ }
                }
                retrievalSw.Stop();
                modelResult.RetrievalTimeMs = Math.Round(retrievalSw.Elapsed.TotalMilliseconds, 2);

                var top3 = scored
                    .OrderByDescending(x => x.Score)
                    .Take(3)
                    .ToList();

                modelResult.TopChunks = top3.Select(x => new PlaygroundChunkResultDto
                {
                    ChunkId = x.Chunk.ChunkID,
                    DocumentID = x.Chunk.DocumentID,
                    DocumentTitle = x.Chunk.Document?.Title ?? "(Unknown)",
                    PageNumber = x.Chunk.PageNumber,
                    CosineSimilarity = Math.Round(Math.Clamp((double)x.Score, 0, 1), 4),
                    ContentSnippet = x.Chunk.ContentText.Length > 350
                        ? x.Chunk.ContentText[..350] + "..."
                        : x.Chunk.ContentText
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error running playground for model {Model}", modelName);
                modelResult.IsAvailable = false;
                modelResult.UnavailableReason = $"Lỗi: {ex.Message}";
            }

            return modelResult;
        }

        private static float DotProduct(float[] a, float[] b)
        {
            float sum = 0f;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++) sum += a[i] * b[i];
            return sum;
        }

        private static float DotProductPartial(float[] a, float[] b, int dim)
        {
            float sum = 0f;
            for (int i = 0; i < dim; i++) sum += a[i] * b[i];
            return sum;
        }
    }
}
