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
    public class OpenAiLlmService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _modelName;
        private readonly ILogger<OpenAiLlmService> _logger;

        public string ProviderName => "OpenAI";
        public string ModelName => _modelName;

        public OpenAiLlmService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiLlmService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? configuration["AppSettings:OpenAI:ApiKey"];
            _modelName = configuration["OpenAI:ModelName"] ?? configuration["AppSettings:OpenAI:ModelName"] ?? "gpt-4o-mini";
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
                _logger.LogWarning("OpenAI API Key is not configured.");
                result.Answer = "Hệ thống chưa được cấu hình API Key của OpenAI. Vui lòng liên hệ quản trị viên hoặc chọn mô hình khác.";
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            string systemPrompt = @"Bạn là trợ lý học tập AI.
Vui lòng trả lời câu hỏi của người dùng CHỈ DỰA TRÊN ngữ cảnh (context) được cung cấp dưới đây.
Nếu thông tin không có trong ngữ cảnh, hãy nói 'Xin lỗi, tôi không tìm thấy thông tin liên quan trong tài liệu học tập.' Không tự bịa thêm thông tin.
Trả lời bằng tiếng Việt, định dạng rõ ràng, ngắn gọn và dễ hiểu.
KHÔNG tự động thêm phần chú thích trích dẫn nguồn ở cuối câu trả lời (như các câu mở ngoặc đơn dạng '(theo tài liệu..., trang...)'), vì hệ thống đã tự động hiển thị các thẻ nguồn ở giao diện bên dưới.";

            double temp = config?.Temperature ?? 0.2;
            int maxTokens = config?.MaxTokens ?? 1024;

            var payload = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = $"{systemPrompt}\n\nNgữ cảnh:\n{context}" },
                    new { role = "user", content = prompt }
                },
                temperature = temp,
                max_tokens = maxTokens
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
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
                    _logger.LogError("OpenAI API error. Status: {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    result.Answer = "Đã xảy ra lỗi khi kết nối tới OpenAI API.";
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

                result.Answer = "Không thể trích xuất câu trả lời từ OpenAI.";
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                _logger.LogError(ex, "Exception while calling OpenAI API");
                result.Answer = "Lỗi nội bộ khi kết nối tới OpenAI API.";
                return result;
            }
        }

        public async Task<string> GenerateDirectAnswerAsync(string prompt, LLMConfig? config = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("OpenAI API Key is not configured.");
                return "Hệ thống chưa được cấu hình API Key của OpenAI.";
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
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API error. Status: {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    return "Đã xảy ra lỗi khi kết nối tới OpenAI API.";
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
                _logger.LogError(ex, "Exception while calling OpenAI API directly");
                return "Lỗi kết nối OpenAI API.";
            }
        }
    }
}
