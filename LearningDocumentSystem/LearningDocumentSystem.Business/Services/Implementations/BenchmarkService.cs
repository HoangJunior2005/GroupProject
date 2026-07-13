using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class BenchmarkService : IBenchmarkService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<BenchmarkService> _logger;

        public BenchmarkService(IUnitOfWork uow, ILogger<BenchmarkService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<BenchmarkDashboardDto> GetDashboardDataAsync()
        {
            var dto = new BenchmarkDashboardDto();

            try
            {
                // Load tất cả messages kèm user
                var allMessages = (await _uow.ChatSessions.GetAllMessagesWithUserAsync()).ToList();
                var assistantMsgs = allMessages.Where(m => m.Role == "assistant").ToList();
                var userMsgs = allMessages.Where(m => m.Role == "user").ToList();

                var providers = assistantMsgs
                    .Where(m => !string.IsNullOrEmpty(m.ProviderName))
                    .Select(m => m.ProviderName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!providers.Any())
                {
                    providers = new List<string> { "Gemini", "OpenAI", "Groq" };
                }

                // ── KPI ──
                dto.TotalSessions = await _uow.ChatSessions.GetTotalSessionCountAsync();
                dto.TotalQueries = userMsgs.Count;
                dto.TotalActiveStudents = allMessages
                    .Where(m => m.Session?.UserID != null)
                    .Select(m => m.Session!.UserID)
                    .Distinct()
                    .Count();

                // ── 1. Tần suất sử dụng Model (Donut) ──
                var assistantMsgsWithProvider = assistantMsgs
                    .Where(m => !string.IsNullOrEmpty(m.ProviderName))
                    .ToList();
                var totalAssistantWithProvider = assistantMsgsWithProvider.Count;
                var modelUsage = assistantMsgsWithProvider
                    .GroupBy(m => m.ProviderName!)
                    .Select(g => new ModelUsageDto
                    {
                        ProviderName = g.Key,
                        ModelName = g.First().ModelName ?? g.Key,
                        TotalQueries = g.Count(),
                        UniqueStudents = g.Select(m => m.Session?.UserID).Distinct().Count(),
                        UsagePercent = totalAssistantWithProvider > 0
                            ? Math.Round((double)g.Count() / totalAssistantWithProvider * 100, 1) : 0
                    })
                    .OrderByDescending(m => m.TotalQueries)
                    .ToList();
                dto.ModelUsage = modelUsage;
                dto.MostUsedModel = modelUsage.FirstOrDefault()?.ProviderName ?? "—";

                // ── 2. Đánh giá Model (Bar) ──
                dto.ModelRatings = assistantMsgs
                    .Where(m => !string.IsNullOrEmpty(m.ProviderName) && m.Feedback.HasValue && m.Feedback != 0)
                    .GroupBy(m => m.ProviderName!)
                    .Select(g =>
                    {
                        int up = g.Count(m => m.Feedback == 1);
                        int down = g.Count(m => m.Feedback == -1);
                        int total = up + down;
                        return new ModelRatingDto
                        {
                            ProviderName = g.Key,
                            ThumbsUp = up,
                            ThumbsDown = down,
                            TotalVotes = total,
                            RatingPercent = total > 0 ? Math.Round((double)up / total * 100, 1) : 0
                        };
                    })
                    .OrderByDescending(r => r.TotalVotes)
                    .ToList();

                // ── 3. Token tiêu thụ theo tháng (Line) ──
                var last6Months = Enumerable.Range(0, 6)
                    .Select(i => DateTime.UtcNow.AddMonths(-i))
                    .OrderBy(d => d)
                    .Select(d => new { Key = $"{d.Year:D4}-{d.Month:D2}", d.Year, d.Month })
                    .ToList();

                var monthlyTokenList = new List<MonthlyTokenDto>();
                foreach (var month in last6Months)
                {
                    foreach (var provider in providers)
                    {
                        var msgs = assistantMsgs
                            .Where(m => string.Equals(m.ProviderName, provider, StringComparison.OrdinalIgnoreCase)
                                && m.CreatedAt.Year == month.Year && m.CreatedAt.Month == month.Month
                                && m.PromptTokens.HasValue)
                            .ToList();

                        monthlyTokenList.Add(new MonthlyTokenDto
                        {
                            Month = month.Key,
                            MonthLabel = $"T{month.Month}/{month.Year}",
                            ProviderName = provider,
                            TotalPromptTokens = msgs.Sum(m => m.PromptTokens ?? 0),
                            TotalCompletionTokens = msgs.Sum(m => m.CompletionTokens ?? 0),
                            QueryCount = msgs.Count
                        });
                    }
                }
                dto.MonthlyTokens = monthlyTokenList;

                // ── 4. Tốc độ phản hồi theo tháng (Line) ──
                var monthlyLatencyList = new List<MonthlyLatencyDto>();
                foreach (var month in last6Months)
                {
                    foreach (var provider in providers)
                    {
                        var msgs = assistantMsgs
                            .Where(m => string.Equals(m.ProviderName, provider, StringComparison.OrdinalIgnoreCase)
                                && m.CreatedAt.Year == month.Year && m.CreatedAt.Month == month.Month
                                && m.ExecutionTimeMs.HasValue && m.ExecutionTimeMs > 0)
                            .ToList();

                        if (msgs.Any())
                        {
                            var times = msgs.Select(m => m.ExecutionTimeMs!.Value).ToList();
                            monthlyLatencyList.Add(new MonthlyLatencyDto
                            {
                                Month = month.Key,
                                MonthLabel = $"T{month.Month}/{month.Year}",
                                ProviderName = provider,
                                AvgLatencyMs = Math.Round(times.Average(), 0),
                                MinLatencyMs = Math.Round(times.Min(), 0),
                                MaxLatencyMs = Math.Round(times.Max(), 0),
                                SampleCount = times.Count
                            });
                        }
                        else
                        {
                            monthlyLatencyList.Add(new MonthlyLatencyDto
                            {
                                Month = month.Key,
                                MonthLabel = $"T{month.Month}/{month.Year}",
                                ProviderName = provider,
                                SampleCount = 0
                            });
                        }
                    }
                }
                dto.MonthlyLatency = monthlyLatencyList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics dashboard data.");
            }

            return dto;
        }

        public Task<List<PlaygroundResultDto>> RunPlaygroundAsync(
            string question, int? subjectId, int? chapterId)
        {
            // Playground đã bỏ — trả rỗng
            return Task.FromResult(new List<PlaygroundResultDto>());
        }
    }
}
