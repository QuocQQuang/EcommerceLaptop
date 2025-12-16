using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Net.Http;
using Microsoft.SemanticKernel.Connectors.OpenAI; // Added
using EcommerceLaptop.Core.Interfaces;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class SemanticKernelEmbeddingService : IEmbeddingService
    {
        private readonly ILlmConfigProvider _configProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        
        public SemanticKernelEmbeddingService(ILlmConfigProvider configProvider, IHttpClientFactory httpClientFactory)
        {
            _configProvider = configProvider;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var generator = await GetEmbeddingGeneratorAsync();
            var embeddings = await generator.GenerateEmbeddingsAsync(new[] { text });
            return embeddings.First().ToArray();
        }

        public async Task<IList<float[]>> GenerateEmbeddingsAsync(IList<string> texts)
        {
            var generator = await GetEmbeddingGeneratorAsync();
            var embeddings = await generator.GenerateEmbeddingsAsync(texts);
            return embeddings.Select(e => e.ToArray()).ToList();
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private async Task<ITextEmbeddingGenerationService> GetEmbeddingGeneratorAsync()
        {
            // Build kernel dynamically based on current configuration
            var config = await _configProvider.GetConfigAsync();
            
            var builder = Kernel.CreateBuilder();

            var httpClient = _httpClientFactory.CreateClient("llm-client");

            if (config.Provider == "Azure")
            {
                builder.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: "text-embedding-3-small", 
                    endpoint: config.BaseUrl!,
                    apiKey: config.ApiKey,
                    httpClient: httpClient
                );
            }
            else if (config.Provider == "Ollama")
            {
                 // Workaround for Ollama using OpenAI connector with custom endpoint
                 if (!string.IsNullOrEmpty(config.BaseUrl))
                 {
                     try 
                     {
                        httpClient.BaseAddress = new System.Uri(config.BaseUrl); 
                     }
                     catch {}
                 }
                 
                 builder.AddOpenAITextEmbeddingGeneration(
                    modelId: "all-minilm",
                    apiKey: "dummy",
                    httpClient: httpClient
                );
            }
            else // Default OpenAI
            {
                 builder.AddOpenAITextEmbeddingGeneration(
                    modelId: "text-embedding-3-small",
                    apiKey: config.ApiKey,
                    httpClient: httpClient
                );
            }

            var kernel = builder.Build();
            return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
