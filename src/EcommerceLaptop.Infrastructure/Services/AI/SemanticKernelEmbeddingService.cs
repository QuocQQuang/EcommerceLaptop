using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Net.Http;
using Microsoft.SemanticKernel.Connectors.OpenAI; // Added
using EcommerceLaptop.Core.Interfaces;
using Microsoft.Extensions.Configuration; // Added

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class SemanticKernelEmbeddingService : IEmbeddingService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        
        public SemanticKernelEmbeddingService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var generator = GetEmbeddingGenerator();
            var embeddings = await generator.GenerateEmbeddingsAsync(new[] { text });
            return embeddings.First().ToArray();
        }

        public async Task<IList<float[]>> GenerateEmbeddingsAsync(IList<string> texts)
        {
            var generator = GetEmbeddingGenerator();
            var embeddings = await generator.GenerateEmbeddingsAsync(texts);
            return embeddings.Select(e => e.ToArray()).ToList();
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private ITextEmbeddingGenerationService GetEmbeddingGenerator()
        {
            // Read Dedicated Embedding Config
            var provider = _configuration["EmbeddingConfig:Provider"] ?? "OpenAI";
            var modelId = _configuration["EmbeddingConfig:ModelId"] ?? "text-embedding-3-small";
            var apiKey = _configuration["EmbeddingConfig:ApiKey"] ?? "";
            var baseUrl = _configuration["EmbeddingConfig:BaseUrl"];

            var builder = Kernel.CreateBuilder();
            var httpClient = _httpClientFactory.CreateClient("llm-client");

            if (provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                 builder.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: modelId, 
                    endpoint: baseUrl!,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
            }
            else if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                 if (!string.IsNullOrEmpty(baseUrl))
                 {
                     try 
                     {
                        httpClient.BaseAddress = new System.Uri(baseUrl); 
                     }
                     catch {}
                 }
                 
                 builder.AddOpenAITextEmbeddingGeneration(
                    modelId: modelId,
                    apiKey: "dummy",
                    httpClient: httpClient
                );
            }
            else // Default OpenAI
            {
                 if (!string.IsNullOrEmpty(baseUrl))
                 {
                     try 
                     {
                        httpClient.BaseAddress = new System.Uri(baseUrl); 
                     }
                     catch {}
                 }

                 builder.AddOpenAITextEmbeddingGeneration(
                    modelId: modelId,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
            }

            var kernel = builder.Build();
            return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
