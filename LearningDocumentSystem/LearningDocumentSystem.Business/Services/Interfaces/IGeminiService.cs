namespace LearningDocumentSystem.Business.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<string> GenerateAnswerAsync(string question, string context);
        Task<string> GenerateDirectAnswerAsync(string prompt, LLMConfig? config = null);
    }
}
