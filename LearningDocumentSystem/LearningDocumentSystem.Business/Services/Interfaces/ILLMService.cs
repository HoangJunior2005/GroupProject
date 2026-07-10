using System.Threading.Tasks;

namespace LearningDocumentSystem.Business.Services.Interfaces
{
    public interface ILLMService
    {
        string ProviderName { get; } // e.g., "OpenAI", "Groq", "Gemini"
        string ModelName { get; }    // e.g., "gpt-4o-mini", "llama-3.1-8b-instant", "gemini-1.5-pro"
        Task<LLMResponseDto> GenerateAnswerAsync(string prompt, string context, LLMConfig? config = null);
        Task<string> GenerateDirectAnswerAsync(string prompt, LLMConfig? config = null);
    }

    public class LLMResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public double ExecutionTimeMs { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
    }

    public class LLMConfig
    {
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 1024;
    }
}
