using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class GroqLlmService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _modelName;
        private readonly ILogger<GroqLlmService> _logger;

        public string ProviderName => "Groq";
        public string ModelName => _modelName;

        public GroqLlmService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqLlmService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Groq:ApiKey"] ?? configuration["AppSettings:Groq:ApiKey"];
            _modelName = configuration["Groq:ModelName"] ?? configuration["AppSettings:Groq:ModelName"] ?? "llama-3.1-8b-instant";
            _logger = logger;
        }

        public async Task<LLMResponseDto> GenerateAnswerAsync(string prompt, string context, LLMConfig? config = null)
        {
            var result = new LLMResponseDto
            {
                ProviderName = ProviderName,
                ModelName = ModelName
            };

            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                sw.Stop();
                _logger.LogWarning("Groq API Key is not configured.");
                result.Answer = "Hệ thống chưa được cấu hình API Key của Groq. Vui lòng liên hệ quản trị viên hoặc chọn mô hình khác.";
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            string systemPrompt = @"Bạn là trợ lý học tập AI của Learning Document System. Nhiệm vụ của bạn là phân tích ngữ cảnh (context) được cung cấp để trả lời câu hỏi hoặc tóm tắt tài liệu.

Quy tắc tối cao:
1. LUÔN LUÔN ưu tiên tìm kiếm thông tin trong ngữ cảnh để trả lời. Kể cả với các câu hỏi ngắn như 'sách này nói về gì', hãy tổng hợp toàn bộ thông tin có trong ngữ cảnh để tóm tắt.
2. CHỈ KHI NÀO câu hỏi là chào hỏi (hello, hi) hoặc bạn CHẮC CHẮN 100% ngữ cảnh không chứa bất kỳ manh mối nào, bạn mới được phép từ chối. Câu từ chối bắt buộc: 'Xin lỗi, tôi không tìm thấy nội dung liên quan trong tài liệu học tập. Bạn thử chọn đúng môn học ở bên trái, hoặc đặt câu hỏi theo sát các khái niệm trong bài giảng nhé!'
3. Trả lời bằng tiếng Việt ngắn gọn, dễ hiểu. Không trích dẫn nguồn ở cuối câu.";

            double temp = config?.Temperature ?? 0.2;
            int maxTokens = config?.MaxTokens ?? 1024;

            var payload = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Dựa vào ngữ cảnh tài liệu sau đây, hãy trả lời câu hỏi.\n\nNgữ cảnh:\n{context}\n\nCâu hỏi: {prompt}" }
                },
                temperature = temp,
                max_tokens = maxTokens
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                sw.Stop();
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Groq API error. Status: {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    result.Answer = "Đã xảy ra lỗi khi kết nối tới Groq API.";
                    return result;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var pTokens))
                        result.PromptTokens = pTokens.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var cTokens))
                        result.CompletionTokens = cTokens.GetInt32();
                }

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var text = choices[0].GetProperty("message").GetProperty("content").GetString();
                    result.Answer = text ?? "Không nhận được phản hồi từ AI.";
                    return result;
                }

                result.Answer = "Không thể trích xuất câu trả lời từ Groq.";
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                _logger.LogError(ex, "Exception while calling Groq API");
                result.Answer = "Lỗi nội bộ khi kết nối tới Groq API.";
                return result;
            }
        }

        public async Task<string> GenerateDirectAnswerAsync(string prompt, LLMConfig? config = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Groq API Key is not configured.");
                return "Hệ thống chưa được cấu hình API Key của Groq.";
            }

            double temp = config?.Temperature ?? 0.1;
            int maxTokens = config?.MaxTokens ?? 1024;

            var payload = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = temp,
                max_tokens = maxTokens
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Groq API error. Status: {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    return "Đã xảy ra lỗi khi kết nối tới Groq API.";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseString);
                var choices = jsonDoc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "Không nhận được phản hồi.";
                }
                return "Không thể trích xuất câu trả lời.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Groq API directly");
                return "Lỗi kết nối Groq API.";
            }
        }
    }
}
