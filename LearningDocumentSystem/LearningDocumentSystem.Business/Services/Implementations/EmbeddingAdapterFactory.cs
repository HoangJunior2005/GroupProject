using LearningDocumentSystem.Business.Services.Interfaces;
using System.Diagnostics;
using System.Text.Json;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    // ============================================================
    // EMBEDDING ADAPTER INTERFACE
    // Mỗi mô hình nhúng implement interface này để plug vào Playground
    // ============================================================

    public interface IEmbeddingAdapter
    {
        string ModelName { get; }
        string ModelType { get; }   // "Native" | "Stub" | "External"
        int Dimensions { get; }
        bool IsAvailable { get; }
        string? UnavailableReason { get; }
        Task<(float[] Vector, double EmbeddingTimeMs)> EmbedAsync(string text);
    }

    // ============================================================
    // ADAPTER 1: TF-IDF (Native — luôn hoạt động)
    // ============================================================
    public class TfIdfAdapter : IEmbeddingAdapter
    {
        private readonly IEmbeddingService _embeddingService;
        public string ModelName => "TF-IDF";
        public string ModelType => "Native";
        public int Dimensions => 512;
        public bool IsAvailable => true;
        public string? UnavailableReason => null;

        public TfIdfAdapter(IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
        }

        public async Task<(float[] Vector, double EmbeddingTimeMs)> EmbedAsync(string text)
        {
            var sw = Stopwatch.StartNew();
            var json = await _embeddingService.GenerateEmbeddingAsync(text);
            sw.Stop();
            var vector = JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
            return (vector, sw.Elapsed.TotalMilliseconds);
        }
    }

    // ============================================================
    // ADAPTER 2: multilingual-e5-base (Stub — cần Python FastAPI service)
    // ============================================================
    public class MultilingualE5Adapter : IEmbeddingAdapter
    {
        private readonly HttpClient? _httpClient;
        private readonly string? _serviceUrl;

        public string ModelName => "multilingual-e5-base";
        public string ModelType => "External";
        public int Dimensions => 768;
        public bool IsAvailable => !string.IsNullOrEmpty(_serviceUrl);
        public string? UnavailableReason => IsAvailable ? null
            : "Chưa cấu hình Python FastAPI service. Thêm 'EmbeddingServices:E5BaseUrl' vào appsettings.json.";

        public MultilingualE5Adapter(HttpClient? httpClient = null, string? serviceUrl = null)
        {
            _httpClient = httpClient;
            _serviceUrl = serviceUrl;
        }

        public async Task<(float[] Vector, double EmbeddingTimeMs)> EmbedAsync(string text)
        {
            if (!IsAvailable || _httpClient == null || string.IsNullOrEmpty(_serviceUrl))
                return (Array.Empty<float>(), 0);

            var sw = Stopwatch.StartNew();
            try
            {
                var payload = JsonSerializer.Serialize(new { text });
                var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_serviceUrl + "/embed", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                sw.Stop();
                var vector = JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
                return (vector, sw.Elapsed.TotalMilliseconds);
            }
            catch
            {
                sw.Stop();
                return (Array.Empty<float>(), sw.Elapsed.TotalMilliseconds);
            }
        }
    }

    // ============================================================
    // ADAPTER 3: PhoBERT-base (Stub)
    // ============================================================
    public class PhoBertAdapter : IEmbeddingAdapter
    {
        private readonly HttpClient? _httpClient;
        private readonly string? _serviceUrl;

        public string ModelName => "PhoBERT-base";
        public string ModelType => "External";
        public int Dimensions => 768;
        public bool IsAvailable => !string.IsNullOrEmpty(_serviceUrl);
        public string? UnavailableReason => IsAvailable ? null
            : "Chưa cấu hình Python FastAPI service. Thêm 'EmbeddingServices:PhoBertUrl' vào appsettings.json.";

        public PhoBertAdapter(HttpClient? httpClient = null, string? serviceUrl = null)
        {
            _httpClient = httpClient;
            _serviceUrl = serviceUrl;
        }

        public async Task<(float[] Vector, double EmbeddingTimeMs)> EmbedAsync(string text)
        {
            if (!IsAvailable || _httpClient == null || string.IsNullOrEmpty(_serviceUrl))
                return (Array.Empty<float>(), 0);

            var sw = Stopwatch.StartNew();
            try
            {
                var payload = JsonSerializer.Serialize(new { text });
                var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_serviceUrl + "/embed", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                sw.Stop();
                var vector = JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
                return (vector, sw.Elapsed.TotalMilliseconds);
            }
            catch
            {
                sw.Stop();
                return (Array.Empty<float>(), sw.Elapsed.TotalMilliseconds);
            }
        }
    }

    // ============================================================
    // ADAPTER 4: bge-m3 (Stub)
    // ============================================================
    public class BgeM3Adapter : IEmbeddingAdapter
    {
        private readonly HttpClient? _httpClient;
        private readonly string? _serviceUrl;

        public string ModelName => "bge-m3";
        public string ModelType => "External";
        public int Dimensions => 1024;
        public bool IsAvailable => !string.IsNullOrEmpty(_serviceUrl);
        public string? UnavailableReason => IsAvailable ? null
            : "Chưa cấu hình Python FastAPI service. Thêm 'EmbeddingServices:BgeM3Url' vào appsettings.json.";

        public BgeM3Adapter(HttpClient? httpClient = null, string? serviceUrl = null)
        {
            _httpClient = httpClient;
            _serviceUrl = serviceUrl;
        }

        public async Task<(float[] Vector, double EmbeddingTimeMs)> EmbedAsync(string text)
        {
            if (!IsAvailable || _httpClient == null || string.IsNullOrEmpty(_serviceUrl))
                return (Array.Empty<float>(), 0);

            var sw = Stopwatch.StartNew();
            try
            {
                var payload = JsonSerializer.Serialize(new { text });
                var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_serviceUrl + "/embed", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                sw.Stop();
                var vector = JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
                return (vector, sw.Elapsed.TotalMilliseconds);
            }
            catch
            {
                sw.Stop();
                return (Array.Empty<float>(), sw.Elapsed.TotalMilliseconds);
            }
        }
    }

    // ============================================================
    // ADAPTER 5: text-embedding-3-small (OpenAI — Stub)
    // ============================================================
    public class OpenAIEmbeddingAdapter : IEmbeddingAdapter
    {
        private readonly HttpClient? _httpClient;
        private readonly string? _apiKey;

        public string ModelName => "text-embedding-3-small";
        public string ModelType => "External";
        public int Dimensions => 1536;
        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);
        public string? UnavailableReason => IsAvailable ? null
            : "Chưa cấu hình OpenAI API Key. Thêm 'EmbeddingServices:OpenAIKey' vào appsettings.json.";

        public OpenAIEmbeddingAdapter(HttpClient? httpClient = null, string? apiKey = null)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public async Task<(float[] Vector, double EmbeddingTimeMs)> EmbedAsync(string text)
        {
            if (!IsAvailable || _httpClient == null || string.IsNullOrEmpty(_apiKey))
                return (Array.Empty<float>(), 0);

            var sw = Stopwatch.StartNew();
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                var payload = JsonSerializer.Serialize(new { input = text, model = "text-embedding-3-small" });
                var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                sw.Stop();
                using var doc = JsonDocument.Parse(json);
                var vectorData = doc.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("embedding");
                var vector = vectorData.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                return (vector, sw.Elapsed.TotalMilliseconds);
            }
            catch
            {
                sw.Stop();
                return (Array.Empty<float>(), sw.Elapsed.TotalMilliseconds);
            }
        }
    }

    // ============================================================
    // FACTORY
    // ============================================================

    public class EmbeddingAdapterFactory
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public EmbeddingAdapterFactory(
            IEmbeddingService embeddingService,
            IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _embeddingService = embeddingService;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public IEmbeddingAdapter GetAdapter(string modelName)
        {
            return modelName switch
            {
                "TF-IDF" => new TfIdfAdapter(_embeddingService),
                "multilingual-e5-base" => new MultilingualE5Adapter(
                    _httpClientFactory.CreateClient(),
                    _config["EmbeddingServices:E5BaseUrl"]),
                "PhoBERT-base" => new PhoBertAdapter(
                    _httpClientFactory.CreateClient(),
                    _config["EmbeddingServices:PhoBertUrl"]),
                "bge-m3" => new BgeM3Adapter(
                    _httpClientFactory.CreateClient(),
                    _config["EmbeddingServices:BgeM3Url"]),
                "text-embedding-3-small" => new OpenAIEmbeddingAdapter(
                    _httpClientFactory.CreateClient(),
                    _config["EmbeddingServices:OpenAIKey"]),
                _ => new TfIdfAdapter(_embeddingService)
            };
        }

        public IEnumerable<IEmbeddingAdapter> GetAllAdapters()
        {
            return new IEmbeddingAdapter[]
            {
                GetAdapter("TF-IDF"),
                GetAdapter("multilingual-e5-base"),
                GetAdapter("PhoBERT-base"),
                GetAdapter("bge-m3"),
                GetAdapter("text-embedding-3-small"),
            };
        }
    }
}
