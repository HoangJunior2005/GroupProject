using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Data.Repositories.Interfaces;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class BenchmarkService : IBenchmarkService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmbeddingService _embeddingService;

        public BenchmarkService(IUnitOfWork uow, IEmbeddingService embeddingService)
        {
            _uow = uow;
            _embeddingService = embeddingService;
        }

        public async Task<BenchmarkDashboardDto> GetDashboardAsync()
        {
            var messages = (await _uow.ChatSessions.GetBenchmarkMessagesAsync()).ToList();
            var chunks = (await _uow.DocumentChunks.GetAllAsync()).ToList();
            var documentCount = chunks.Select(c => c.DocumentID).Distinct().Count();
            var rated = messages.Where(m => m.Feedback.HasValue).ToList();

            return new BenchmarkDashboardDto
            {
                TotalQuestions = messages.Count,
                AverageLatencyMs = messages.Where(m => m.ExecutionTimeMs.HasValue)
                    .Select(m => m.ExecutionTimeMs!.Value).DefaultIfEmpty().Average(),
                TotalTokens = messages.Sum(m => (m.PromptTokens ?? 0) + (m.CompletionTokens ?? 0)),
                RatedResponses = rated.Count,
                HelpfulnessRate = rated.Count == 0 ? 0 : rated.Count(m => m.Feedback == 1) * 100d / rated.Count,
                TotalDocuments = documentCount,
                TotalChunks = chunks.Count,
                AverageChunksPerDocument = documentCount == 0 ? 0 : chunks.Count / (double)documentCount,
                AverageChunkLength = chunks.Select(c => c.ContentText?.Length ?? 0).DefaultIfEmpty().Average(),
                Providers = messages
                    .GroupBy(m => new { Provider = m.ProviderName ?? "Unknown", Model = m.ModelName ?? "Unknown" })
                    .Select(g =>
                    {
                        var providerRated = g.Where(m => m.Feedback.HasValue).ToList();
                        return new LlmBenchmarkDto
                        {
                            ProviderName = g.Key.Provider,
                            ModelName = g.Key.Model,
                            RequestCount = g.Count(),
                            AverageLatencyMs = g.Where(m => m.ExecutionTimeMs.HasValue)
                                .Select(m => m.ExecutionTimeMs!.Value).DefaultIfEmpty().Average(),
                            AveragePromptTokens = g.Select(m => (double)(m.PromptTokens ?? 0)).DefaultIfEmpty().Average(),
                            AverageCompletionTokens = g.Select(m => (double)(m.CompletionTokens ?? 0)).DefaultIfEmpty().Average(),
                            RatedCount = providerRated.Count,
                            HelpfulnessRate = providerRated.Count == 0
                                ? 0
                                : providerRated.Count(m => m.Feedback == 1) * 100d / providerRated.Count
                        };
                    })
                    .OrderByDescending(x => x.RequestCount)
                    .ToList(),
                RecentResponses = messages.Take(10).Select(m => new RecentBenchmarkDto
                {
                    ProviderName = m.ProviderName ?? "Unknown",
                    ModelName = m.ModelName ?? "Unknown",
                    ExecutionTimeMs = m.ExecutionTimeMs,
                    TotalTokens = (m.PromptTokens ?? 0) + (m.CompletionTokens ?? 0),
                    Feedback = m.Feedback,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };
        }

        public async Task<BenchmarkRetrievalDto> RunRetrievalAsync(string question, int? subjectId = null, int? chapterId = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.", nameof(question));

            var stopwatch = Stopwatch.StartNew();
            var questionJson = await _embeddingService.GenerateEmbeddingAsync(question.Trim());
            var questionVector = JsonSerializer.Deserialize<float[]>(questionJson) ?? Array.Empty<float>();
            var chunks = (await _uow.DocumentChunks.GetChunksForRAGAsync(subjectId, chapterId)).ToList();
            var scored = new List<(float Score, Entities.Models.DocumentChunk Chunk)>();

            foreach (var chunk in chunks)
            {
                if (chunk.Embedding == null || string.IsNullOrWhiteSpace(chunk.Embedding.VectorData))
                    continue;

                try
                {
                    var chunkVector = JsonSerializer.Deserialize<float[]>(chunk.Embedding.VectorData);
                    if (chunkVector == null || chunkVector.Length != questionVector.Length)
                        continue;

                    scored.Add((DotProduct(questionVector, chunkVector) + ComputeKeywordBoost(question, chunk.ContentText), chunk));
                }
                catch (JsonException)
                {
                    // Ignore malformed legacy vectors and continue the experiment.
                }
            }

            stopwatch.Stop();
            return new BenchmarkRetrievalDto
            {
                ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                ScannedChunks = chunks.Count,
                Results = scored.OrderByDescending(x => x.Score).Take(5).Select(x => new BenchmarkRetrievalResultDto
                {
                    DocumentID = x.Chunk.DocumentID,
                    DocumentTitle = x.Chunk.Document.Title,
                    ChapterName = x.Chunk.Document.Chapter.ChapterName,
                    PageNumber = x.Chunk.PageNumber,
                    SimilarityScore = Math.Clamp(x.Score * 100f, 0f, 100f),
                    ContentSnippet = x.Chunk.ContentText.Length > 260
                        ? x.Chunk.ContentText[..260] + "..."
                        : x.Chunk.ContentText
                }).ToList()
            };
        }

        private static float DotProduct(float[] a, float[] b)
        {
            float sum = 0;
            for (var i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }

        private static float ComputeKeywordBoost(string question, string content)
        {
            var terms = Regex.Matches(question.ToLowerInvariant(), @"[\p{L}\p{N}]+")
                .Select(m => m.Value)
                .Where(x => x.Length > 2)
                .Distinct()
                .ToList();

            if (terms.Count == 0) return 0;
            var normalizedContent = content.ToLowerInvariant();
            return terms.Count(t => normalizedContent.Contains(t)) / (float)terms.Count * 0.35f;
        }
    }
}
