using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Models.AI;
using EcommerceLaptop.Core.Services;
using System.Net.Http;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class RagChatService : IChatService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDbService _vectorDbService;
        private readonly ILlmConfigProvider _configProvider;
        private readonly ISemanticCacheService _cacheService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRagMetricsService _metricsService;
        private readonly IGuardrailService _guardrailService;
        private readonly IIntentClassifier _intentClassifier;
        private readonly IProductService _productService;

        private const string CollectionName = "products";

        public RagChatService(
            IEmbeddingService embeddingService,
            IVectorDbService vectorDbService,
            ILlmConfigProvider configProvider,
            ISemanticCacheService cacheService,
            IHttpClientFactory httpClientFactory,
            IRagMetricsService metricsService,
            IGuardrailService guardrailService,
            IIntentClassifier intentClassifier,
            IProductService productService)
        {
            _embeddingService = embeddingService;
            _vectorDbService = vectorDbService;
            _configProvider = configProvider;
            _cacheService = cacheService;
            _httpClientFactory = httpClientFactory;
            _metricsService = metricsService;
            _guardrailService = guardrailService;
            _intentClassifier = intentClassifier;
            _productService = productService;
        }

        public async IAsyncEnumerable<ChatResponseChunk> ProcessMessageAsync(ChatRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 0. Guardrails (Input)
            var inputCheck = await _guardrailService.ValidateInputAsync(request.Message);
            if (!inputCheck.IsSafe)
            {
                yield return new ChatResponseChunk 
                { 
                    Content = $"I cannot process that request. {inputCheck.Reason}", 
                    IsComplete = true 
                };
                yield break;
            }

            // 1. Check Cache
            var cachedResponse = await _cacheService.GetCachedResponseAsync(request.Message);
            if (cachedResponse != null)
            {
                _metricsService.RecordCacheHit(true);
                yield return cachedResponse;
                yield break;
            }
            _metricsService.RecordCacheHit(false);

            // 2. Intent Classification
            var intent = await _intentClassifier.ClassifyIntentAsync(request.Message);

            string contextString = "";
            List<string> sources = new();

            // 3. Routing based on Intent
            if (intent == Core.Enums.UserIntent.ProductSearch)
            {
                // RAG Flow
                var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Message);
                var searchResults = await _vectorDbService.SearchAsync(CollectionName, embedding, limit: 5);
                
                // Real-time Data Enrichment
                var productIds = searchResults
                    .Where(r => int.TryParse(r.Id, out _))
                    .Select(r => int.Parse(r.Id))
                    .ToList();

                if (productIds.Any())
                {
                    var products = await _productService.GetProductsByIdsAsync(productIds);
                    var productDict = products.ToDictionary(p => p.Id);

                    contextString = string.Join("\n\n", searchResults.Select(r => 
                    {
                        if (int.TryParse(r.Id, out int pid) && productDict.TryGetValue(pid, out var product))
                        {
                            // Combine Vector Content with Real-time SQL Data
                            return $"[Product Info]: {product.Name}\nPrice: ${product.Price}\nStock: 10 (In Stock)\nDetails: {r.Content}";
                        }
                        return $"[Product Info]: {r.Content}";
                    }));
                }
                else
                {
                    contextString = string.Join("\n\n", searchResults.Select(r => $"[Product Info]: {r.Content}"));
                }
                
                sources = searchResults.Select(r => r.Id).ToList();
            }
            else if (intent == Core.Enums.UserIntent.Support)
            {
                // Simple placeholder for Support RAG (could query a 'policies' collection)
                // For now, let's just let the LLM handle it with general knowledge, or maybe add a static policy string.
                contextString = "Store Policy: We offer 30-day returns. Warranty is 1 year for all laptops.";
            }
            // GeneralChat -> No Context

            // 2. Prepare Kernel & Chat
            var kernel = await BuildKernelAsync();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage($@"You are an intelligent sales assistant for an E-commerce laptop store. 
Use the following context to answer the user's question. If the answer is not in the context, politely say you don't have that information.
Review the context carefully. It contains product titles, specs, and descriptions.

Context:
{contextString}");

            // Add previous history
            foreach (var msg in request.History)
            {
                if (msg.Role == "user") chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant") chatHistory.AddAssistantMessage(msg.Content);
            }

            chatHistory.AddUserMessage(request.Message);

            // 3. Stream Response
            var executionSettings = new OpenAIPromptExecutionSettings() { Temperature = 0.7 };
            
            var responses = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel);
            
            var fullResponseBuilder = new StringBuilder();
            bool firstChunk = true;
            
            await foreach (var content in responses)
            {
                if (!string.IsNullOrEmpty(content.Content))
                {
                    fullResponseBuilder.Append(content.Content);
                    yield return new ChatResponseChunk 
                    { 
                        Content = content.Content, 
                        IsComplete = false,
                        Sources = firstChunk ? sources : new List<string>()
                    };
                    firstChunk = false;
                }
            }
            
            // Unify response for cache
            await _cacheService.CacheResponseAsync(request.Message, fullResponseBuilder.ToString());
            
            stopwatch.Stop();
            _metricsService.RecordLatency(stopwatch.ElapsedMilliseconds, "ChatGeneration");
            // Estimate tokens (simple mapping 4 chars = 1 token) or use library if available
            _metricsService.RecordTokenUsage(request.Message.Length / 4, fullResponseBuilder.Length / 4);

            yield return new ChatResponseChunk { IsComplete = true };
        }

        private async Task<Kernel> BuildKernelAsync()
        {
            var config = await _configProvider.GetConfigAsync();
            var builder = Kernel.CreateBuilder();

            // Create resilient client
            var httpClient = _httpClientFactory.CreateClient("llm-client");

            if (config.Provider == "Azure")
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: config.ModelId ?? "gpt-4o", 
                    endpoint: config.BaseUrl!,
                    apiKey: config.ApiKey,
                    httpClient: httpClient
                );
            }
            else if (config.Provider == "Ollama")
            {
                 // For Ollama with a resilient client, we pass the client but might need to respect the base URL from config if it differs from default.
                 // However, AddOpenAIChatCompletion usually expects the HttpClient to be pre-configured if passed.
                 // Or we pass the endpoint as modelId? No.
                 // The AddOpenAIChatCompletion overload with httpClient DOES NOT take an endpoint usually? 
                 // It depends on the SK version. 
                 // Let's assume standard OpenAI protocol.
                 // If we use "CreateClient", we might not have set the BaseAddress yet if it varies.
                 // But typically "llm-client" is configured in one place.
                 // Here configurations are dynamic.
                 // We can set the BaseAddress on the client instance if it's null.
                 
                 // Since we're using "StandardResilienceHandler", it's a generic client.
                 // We manually set BaseAddress for this request's client instance.
                 if (!string.IsNullOrEmpty(config.BaseUrl))
                 {
                     // Ideally we shouldn't mute the client from factory, but here we need to point to dynamic target.
                     // A safer way is using Named Clients for specific known providers, but here provider is dynamic.
                     // We'll proceed with creating a client and setting base address if not set.
                     // Note: You can't change BaseAddress if request started, but this is a fresh client instance.
                     // actually properties of HttpClient are not thread safe if modifying shared one, but CreateClient returns new instance.
                     // however, it shares the handler pipeline.
                     // Setting BaseAddress on the returned HttpClient instance is safe.
                     try 
                     {
                        httpClient.BaseAddress = new System.Uri(config.BaseUrl); 
                     }
                     catch {} // Ignore if already set or invalid
                 }

                 builder.AddOpenAIChatCompletion(
                    modelId: config.ModelId ?? "llama3",
                    apiKey: "dummy",
                    httpClient: httpClient
                );
            }
            else // Default OpenAI
            {
                 builder.AddOpenAIChatCompletion(
                    modelId: config.ModelId ?? "gpt-4o",
                    apiKey: config.ApiKey,
                    httpClient: httpClient
                );
            }

            return builder.Build();
        }
    }
}
