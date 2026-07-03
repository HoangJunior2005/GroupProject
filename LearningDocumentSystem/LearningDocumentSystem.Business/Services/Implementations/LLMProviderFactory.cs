using LearningDocumentSystem.Business.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public interface ILLMProviderFactory
    {
        ILLMService GetProvider(string? providerName = null);
        IEnumerable<string> GetAvailableProviders();
    }

    public class LLMProviderFactory : ILLMProviderFactory
    {
        private readonly IEnumerable<ILLMService> _services;

        public LLMProviderFactory(IEnumerable<ILLMService> services)
        {
            _services = services;
        }

        public ILLMService GetProvider(string? providerName = null)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                // Default to Gemini or first available
                return _services.FirstOrDefault(s => string.Equals(s.ProviderName, "Gemini", StringComparison.OrdinalIgnoreCase))
                       ?? _services.First();
            }

            var service = _services.FirstOrDefault(s => string.Equals(s.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
            if (service == null)
            {
                // Fallback if provider not found
                return _services.FirstOrDefault(s => string.Equals(s.ProviderName, "Gemini", StringComparison.OrdinalIgnoreCase))
                       ?? _services.First();
            }

            return service;
        }

        public IEnumerable<string> GetAvailableProviders()
        {
            return _services.Select(s => s.ProviderName).Distinct();
        }
    }
}
