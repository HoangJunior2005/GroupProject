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

            string systemPrompt = @"Bạn là trợ lý học tập AI của hệ thống Learning Document System.
Quy tắc trả lời:
1. GIAO TIẾP XÃ GIAO: Nếu người dùng chào hỏi (như 'xin chào', 'hello', 'chào bạn', 'hi',...) hoặc nói cảm ơn/khen ngợi, hãy đáp lại thân thiện, lịch sự và tự giới thiệu bạn là trợ lý AI học tập sẵn sàng hỗ trợ giải đáp kiến thức theo tài liệu bài giảng.
2. CÂU HỎI KIẾN THỨC / CHUYÊN MÔN: Vui lòng trả lời CHỈ DỰA TRÊN ngữ cảnh (context) được cung cấp dưới đây.
3. CÂU HỎI NGOÀI LUỒNG: Nếu người dùng hỏi các câu hỏi chuyên môn/kiến thức mà thông tin KHÔNG CÓ trong ngữ cảnh, hãy nói rõ: 'Xin lỗi, tôi không tìm thấy thông tin liên quan trong tài liệu học tập. Vui lòng đặt câu hỏi theo sát các tài liệu hoặc bài giảng trong hệ thống nhé!'. KHÔNG tự bịa hoặc dùng kiến thức bên ngoài để trả lời các câu hỏi chuyên môn ngoài phạm vi tài liệu.
4. Trả lời bằng tiếng Việt, định dạng rõ ràng, ngắn gọn và dễ hiểu. KHÔNG tự động thêm phần chú thích trích dẫn nguồn ở cuối câu trả lời (vì hệ thống đã tự động hiển thị các thẻ nguồn ở giao diện bên dưới).
5. BÁM SÁT MÔN HỌC / CHƯƠNG ĐANG CHỌN: Khi người dùng hỏi về kiến thức, tổng quan hay nội dung của một chương hoặc môn học, PHẢI trả lời bám sát trực tiếp vào kiến thức chuyên môn của môn học và chương đó (dựa trên ngữ cảnh được cung cấp hoặc phạm vi môn học đang chọn). Tuyệt đối KHÔNG được trả lời lan man sang các chủ đề không liên quan như 'lộ trình học tập cá nhân hóa', 'công cụ học tập tương tác', 'theo dõi tiến độ', hay các tính năng chung của hệ thống phần mềm.";

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
