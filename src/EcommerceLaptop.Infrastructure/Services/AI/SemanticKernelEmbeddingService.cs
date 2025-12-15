using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI; // Added
using EcommerceLaptop.Core.Interfaces;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class SemanticKernelEmbeddingService : IEmbeddingService
    {
        private readonly ILlmConfigProvider _configProvider;
        
        public SemanticKernelEmbeddingService(ILlmConfigProvider configProvider)
        {
            _configProvider = configProvider;
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

            if (config.Provider == "Azure")
            {
                builder.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: "text-embedding-3-small", 
                    endpoint: config.BaseUrl!,
                    apiKey: config.ApiKey
                );
            }
            else if (config.Provider == "Ollama")
            {
                 // Workaround for Ollama using OpenAI connector with custom endpoint
                 // Note: AddOpenAITextEmbeddingGeneration does not support endpoint in some versions.
                 // We might need to use HttpClient or specific constructor if API allows.
                 // For now, let's assume OpenAI connector is used for standard OpenAI.
                 // If Ollama is needed, we usually point BaseAddress of HttpClient.
                 
                 builder.AddOpenAITextEmbeddingGeneration(
                    modelId: "all-minilm",
                    apiKey: "dummy",
                    httpClient: new System.Net.Http.HttpClient { BaseAddress = new Uri(config.BaseUrl ?? "http://localhost:11434/v1") }
                );
            }
            else // Default OpenAI
            {
                 builder.AddOpenAITextEmbeddingGeneration(
                    modelId: "text-embedding-3-small",
                    apiKey: config.ApiKey
                );
            }

            var kernel = builder.Build();
            return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
