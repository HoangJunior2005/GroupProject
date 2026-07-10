using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class GeminiService : IGeminiService, ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _modelName;
        private readonly ILogger<GeminiService> _logger;

        public string ProviderName => "Gemini";
        public string ModelName => _modelName;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"];
            _modelName = configuration["Gemini:ModelName"] ?? "gemini-2.5-flash";
            _logger = logger;
        }

        // Implementation of IGeminiService
        public async Task<string> GenerateAnswerAsync(string question, string context)
        {
            var res = await ((ILLMService)this).GenerateAnswerAsync(question, context);
            return res.Answer;
        }

        // Implementation of ILLMService
        async Task<LLMResponseDto> ILLMService.GenerateAnswerAsync(string prompt, string context, LLMConfig? config)
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
                _logger.LogWarning("Gemini API Key is not configured. Falling back to default message.");
                result.Answer = "Hệ thống chưa được cấu hình API Key của Gemini. Vui lòng liên hệ quản trị viên.";
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            string systemPrompt = @"Bạn là trợ lý học tập AI của hệ thống Learning Document System.
Quy tắc trả lời:
1. CÂU HỎI KIẾN THỨC / CHUYÊN MÔN: Vui lòng trả lời CHỈ DỰA TRÊN ngữ cảnh (context) tài liệu học tập được cung cấp dưới đây.
2. CÂU HỎI NGOÀI LUỒNG & CHÀO HỎI: TUYỆT ĐỐI KHÔNG trả lời các câu hỏi ngoài lề, câu chào hỏi xã giao hoặc kiến thức không nằm trong ngữ cảnh tài liệu bài giảng (như xin chào, alo, hello, hi, thời tiết, thể thao, lập trình chung, tin tức, văn học, đời sống...). Nếu người dùng hỏi ngoài lề, chào hỏi hoặc kiến thức không có trong tài liệu, BẮT BUỘC phải trả lời chính xác câu thông báo sau: 'Xin lỗi, tôi không tìm thấy nội dung liên quan trong tài liệu học tập. Bạn thử chọn đúng môn học ở bên trái, hoặc đặt câu hỏi theo sát các khái niệm trong bài giảng nhé!'. KHÔNG tự ý bịa đặt hoặc dùng kiến thức bên ngoài của bạn để trả lời.
3. Trả lời bằng tiếng Việt, định dạng rõ ràng, ngắn gọn và dễ hiểu. KHÔNG tự động thêm phần chú thích trích dẫn nguồn ở cuối câu trả lời (vì hệ thống đã tự động hiển thị các thẻ nguồn ở giao diện bên dưới).
4. BÁM SÁT MÔN HỌC / CHƯƠNG ĐANG CHỌN: Khi người dùng hỏi về kiến thức, tổng quan hay nội dung của một chương hoặc môn học, PHẢI trả lời bám sát trực tiếp vào kiến thức chuyên môn của môn học và chương đó (dựa trên ngữ cảnh được cung cấp hoặc phạm vi môn học đang chọn). Tuyệt đối KHÔNG được trả lời lan man sang các chủ đề không liên quan như 'lộ trình học tập cá nhân hóa', 'công cụ học tập tương tác', 'theo dõi tiến độ', hay các tính năng chung của hệ thống phần mềm.

Ngữ cảnh:
";
            
            string fullPrompt = $"{systemPrompt}\n{context}\n\nCâu hỏi: {prompt}\nTrả lời:";

            double temp = config?.Temperature ?? 0.2;
            int maxTokens = config?.MaxTokens ?? 1024;

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = fullPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = temp,
                    maxOutputTokens = maxTokens
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                sw.Stop();
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error. Status: {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    result.Answer = "Đã xảy ra lỗi khi kết nối tới mô hình AI.";
                    return result;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                // Parse token usage metadata if available
                if (root.TryGetProperty("usageMetadata", out var usage))
                {
                    if (usage.TryGetProperty("promptTokenCount", out var promptTokens))
                        result.PromptTokens = promptTokens.GetInt32();
                    if (usage.TryGetProperty("candidatesTokenCount", out var completionTokens))
                        result.CompletionTokens = completionTokens.GetInt32();
                }

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                        
                    result.Answer = text ?? "Không nhận được phản hồi từ AI.";
                    return result;
                }

                result.Answer = "Không thể trích xuất câu trả lời từ hệ thống.";
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                _logger.LogError(ex, "Exception while calling Gemini API");
                result.Answer = "Lỗi nội bộ khi xử lý câu trả lời với AI.";
                return result;
            }
        }

        public async Task<string> GenerateDirectAnswerAsync(string prompt, LLMConfig? config = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Gemini API Key is not configured.");
                return "Hệ thống chưa được cấu hình API Key của Gemini.";
            }

            double temp = config?.Temperature ?? 0.1;
            int maxTokens = config?.MaxTokens ?? 1024;

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = temp,
                    maxOutputTokens = maxTokens
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error. Status: {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    return "Đã xảy ra lỗi khi kết nối tới mô hình AI.";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                using var jsonDoc = JsonDocument.Parse(responseString);
                var candidates = jsonDoc.RootElement.GetProperty("candidates");
                
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                        
                    return text ?? "Không nhận được phản hồi từ AI.";
                }

                return "Không thể trích xuất câu trả lời từ hệ thống.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Gemini API directly");
                return "Lỗi nội bộ khi xử lý câu trả lời với AI.";
            }
        }
    }
}
