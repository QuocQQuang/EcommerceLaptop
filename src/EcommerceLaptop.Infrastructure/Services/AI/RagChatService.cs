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
using System.Runtime.CompilerServices;
using EcommerceLaptop.Core.DTOs.Chat;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;

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
        private readonly IChatPersistenceService _persistenceService;
        private readonly ILogger<RagChatService> _logger;

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
            IProductService productService,
            IChatPersistenceService persistenceService,
            ILogger<RagChatService> logger)
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
            _persistenceService = persistenceService;
            _logger = logger;
        }

        public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
            string query,
            string? sessionId = null,
            string? userId = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int tokenCount = 0;

            // Persistence: Ensure Session ID
            if (string.IsNullOrEmpty(sessionId))
            {
                 // Create new session
                 string title = query.Length > 50 ? query.Substring(0, 47) + "..." : query;
                 // If userId is null, treat as anonymous/guest (or handle as per auth policy)
                 // integrating logic: if userId is null, we can't persist effectively for a user, but we can for a temporary session.
                 // For now, we allow null userId but maybe generate a temp ID?
                 var effectiveUserId = userId ?? "anonymous"; 
                 sessionId = await _persistenceService.CreateSessionAsync(effectiveUserId, title);
            }

            yield return new ProgressEvent { Stage = "Validation", Message = "Validating input...", Progress = 0.1 };

            // 0. Guardrails (Input)
            var inputCheck = await _guardrailService.ValidateInputAsync(query);
            if (!inputCheck.IsSafe)
            {
                yield return new ErrorEvent 
                { 
                    Code = "UNSAFE_INPUT", 
                    Message = $"I cannot process that request. {inputCheck.Reason}",
                    Recoverable = false 
                };
                yield break;
            }

            // 1. Check Cache
            // yield return new ProgressEvent { Stage = "Cache", Message = "Checking cache...", Progress = 0.2 };
            // var cachedResponse = await _cacheService.GetCachedResponseAsync(query);
            // if (cachedResponse != null)
            // {
            //     _metricsService.RecordCacheHit(true);
            //     yield return new MetadataEvent { ProcessingStage = "Cache", CacheHit = true, ElapsedMs = stopwatch.ElapsedMilliseconds };
                
            //     // Stream cached response as a single big token or simulate streaming?
            //     // For better UX, usually we just return it. 
            //     // But the protocol expects TokenEvents.
            //     // We'll treat the whole cached content as one token for simplicity, or split it if we want "streaming" feel.
            //     // yield return new TokenEvent { Token = cachedResponse.Content, Index = 0 };
                
            //     // yield return new CompleteEvent 
            //     // { 
            //     //     QueryId = System.Guid.NewGuid().ToString(),
            //     //     TotalTokens = cachedResponse.Content.Length / 4, // Estimate
            //     //     DurationMs = stopwatch.ElapsedMilliseconds,
            //     //     CacheHit = true,
            //     //     ModelUsed = "Cache"
            //     // };
                
            //     // // Save interaction to history even if cached?
            //     // // Yes, otherwise context is lost.
            //     // await _persistenceService.SaveMessageAsync(sessionId, "user", query);
            //     // await _persistenceService.SaveMessageAsync(sessionId, "assistant", cachedResponse.Content);
                
            //     // yield break;
            // }
            _logger.LogWarning("!!! NEW CODE RUNNING - CACHE DISABLED !!!");
            _metricsService.RecordCacheHit(false);

            // 2. Intent Classification
            yield return new ProgressEvent { Stage = "Intent", Message = "Understanding query...", Progress = 0.3 };
            var intent = await _intentClassifier.ClassifyIntentAsync(query);
            
            string contextString = "";
            List<string> sources = new();

            // 3. Routing based on Intent
            Console.WriteLine($"[DEBUG] Intent: {intent}");
            if (intent == Core.Enums.UserIntent.ProductSearch)
            {
                yield return new ProgressEvent { Stage = "Retrieval", Message = "Searching products...", Progress = 0.5 };
                
                List<ProductEvent> foundProducts = new();
                string? retrievalError = null;

                try
                {
                    // RAG Flow
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
                    var searchResults = await _vectorDbService.SearchAsync(CollectionName, embedding, limit: 5);
                    
                    // Real-time Data Enrichment
                    var productIds = searchResults
                        .Select(r => 
                        {
                            _logger.LogWarning($"[DEBUG] Processing result {r.Id}. Metadata Keys: {string.Join(", ", r.Metadata.Keys)}");
                            if (r.Metadata.TryGetValue("product_id", out var pidObj))
                            {
                                _logger.LogWarning($"[DEBUG] Found product_id: {pidObj} ({pidObj.GetType().Name})");
                                if (pidObj is int i) return i;
                                if (pidObj is long l) return (int)l;
                                if (pidObj is string s && int.TryParse(s, out var parsed)) return parsed;
                            }
                            else 
                            {
                                _logger.LogWarning($"[DEBUG] product_id NOT FOUND in metadata for result {r.Id}");
                            }
                            return (int?)null;
                        })
                        .Where(pid => pid.HasValue)
                        .Select(pid => pid.Value)
                        .Distinct()
                        .ToList();

                    _logger.LogWarning($"[DEBUG] Extracted Product IDs: {string.Join(", ", productIds)}");

                    if (productIds.Any())
                    {
                        var products = await _productService.GetProductsByIdsAsync(productIds);
                        _logger.LogWarning($"[DEBUG] Retrieved {products.Count()} products from DB.");
                        var productDict = products.ToDictionary(p => p.Id);

                        // Collect Product Events
                        int rank = 1;
                        foreach (var result in searchResults)
                        {
                            int? pid = null;
                            if (result.Metadata.TryGetValue("product_id", out var pidObj))
                            {
                                if (pidObj is int i) pid = i;
                                else if (pidObj is long l) pid = (int)l;
                                else if (pidObj is string s && int.TryParse(s, out var parsed)) pid = parsed;
                            }

                             if (pid.HasValue && productDict.TryGetValue(pid.Value, out var product))
                             {
                                 foundProducts.Add(new ProductEvent
                                 {
                                     Id = product.Id.ToString(),
                                     Name = product.Name,
                                     Description = product.Description,
                                     Price = product.Price,
                                     RelevanceScore = result.Score,
                                     ImageUrl = product.Images?.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).FirstOrDefault()?.ImageUrl,
                                     Slug = GenerateSlugFromName(product.Name),
                                     Rank = rank++
                                 });
                             }
                        }

                        contextString = string.Join("\n\n", searchResults.Select(r => 
                        {
                            int? pid = null;
                            if (r.Metadata.TryGetValue("product_id", out var pidObj))
                            {
                                if (pidObj is int i) pid = i;
                                else if (pidObj is long l) pid = (int)l;
                                else if (pidObj is string s && int.TryParse(s, out var parsed)) pid = parsed;
                            }

                            if (pid.HasValue && productDict.TryGetValue(pid.Value, out var product))
                            {
                                return $"[Product Info]: {product.Name}\nPrice: ${product.Price}\nStock: 10 (In Stock)\nDetails: {r.Content}";
                            }
                            return $"[Product Info]: {r.Content}";
                        }));
                            if (r.Metadata.TryGetValue("product_id", out var pidObj))
                            {
                                if (pidObj is int i) pid = i;
                                else if (pidObj is long l) pid = (int)l;
                                else if (pidObj is string s && int.TryParse(s, out var parsed)) pid = parsed;
                            }

                            if (pid.HasValue && productDict.TryGetValue(pid.Value, out var product))
                            {
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
                catch (Exception ex)
                {
                    retrievalError = ex.Message;
                }

                if (retrievalError != null)
                {
                    yield return new ProgressEvent { Stage = "Retrieval_Failed", Message = $"Search failed: {retrievalError}. Falling back to general knowledge.", Progress = 0.5 };
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Found {foundProducts.Count} products.");
                    foreach (var p in foundProducts)
                    {
                        yield return p;
                    }
                }
            }
            else if (intent == Core.Enums.UserIntent.Support)
            {
                contextString = "Store Policy: We offer 30-day returns. Warranty is 1 year for all laptops. Shipping is free for orders over $500.";
            }

            yield return new ProgressEvent { Stage = "Generation", Message = "Generating response...", Progress = 0.8 };

            // 4. Prepare Kernel & Chat
            var kernel = await BuildKernelAsync();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage($@"You are an intelligent sales assistant for an E-commerce laptop store. 
Use the following context to answer the user's question. If the answer is not in the context, politely say you don't have that information.
Review the context carefully. It contains product titles, specs, and descriptions.

Context:
{contextString}");

            // Load History
            var history = await _persistenceService.GetSessionHistoryAsync(sessionId);
            foreach (var msg in history)
            {
                if (msg.Role == "user") chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant") chatHistory.AddAssistantMessage(msg.Content);
            }
            
            // Save current user message
            await _persistenceService.SaveMessageAsync(sessionId, "user", query);
            chatHistory.AddUserMessage(query);

            var executionSettings = new OpenAIPromptExecutionSettings() { Temperature = 0.7 };
            var responses = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
            string fullResponse = "";

            // Resilience: Manual enumeration to handle exceptions gracefully (yield not allowed in try-catch)
            var enumerator = responses.GetAsyncEnumerator(cancellationToken);
            try
            {
                var fullResponseBuilder = new StringBuilder();
                int index = 0;

                while (true)
                {
                    bool hasNext = false;
                    Exception? iterationException = null;

                    try
                    {
                        hasNext = await enumerator.MoveNextAsync();
                    }
                    catch (Exception ex)
                    {
                        iterationException = ex;
                    }

                    if (iterationException != null)
                    {
                         // Log error if logger available, or just fallback
                         // _metricsService.RecordError("ChatStreamError", iterationException.Message); 
                         
                         yield return new ErrorEvent
                         {
                             Code = "LLM_SERVICE_UNAVAILABLE",
                             Message = "I'm having trouble connecting to my brain right now. Please try again in a moment.",
                             Recoverable = true
                         };
                         yield break;
                    }

                    if (!hasNext) break;

                    var content = enumerator.Current;
                    if (!string.IsNullOrEmpty(content.Content))
                    {
                        fullResponseBuilder.Append(content.Content);
                        yield return new TokenEvent 
                        { 
                            Token = content.Content, 
                            Index = index++
                        };
                        tokenCount++;
                    }
                }
                
                fullResponse = fullResponseBuilder.ToString();
                
                // Save assistant response
                if (!string.IsNullOrEmpty(fullResponse))
                {
                     await _persistenceService.SaveMessageAsync(sessionId, "assistant", fullResponse);
                     await _cacheService.CacheResponseAsync(query, fullResponse);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
            
            stopwatch.Stop();
            _metricsService.RecordLatency(stopwatch.ElapsedMilliseconds, "ChatGeneration");
            _metricsService.RecordTokenUsage(query.Length / 4, fullResponse.Length / 4);

            yield return new CompleteEvent 
            {
                QueryId = System.Guid.NewGuid().ToString(),
                TotalTokens = tokenCount,
                DurationMs = stopwatch.ElapsedMilliseconds,
                CacheHit = false,
                ModelUsed = "GPT-4o" 
            };
            
            // Signal frontend to stop typing indicator
            yield return new ProgressEvent { Stage = "Done", Message = "completed", Progress = 1.0 };
        }

        private async Task<Kernel> BuildKernelAsync()
        {
            var config = await _configProvider.GetConfigAsync();
            var builder = Kernel.CreateBuilder();

            var httpClient = _httpClientFactory.CreateClient("llm-client");

                if (config.BaseUrl?.Contains("openrouter.ai") == true)
                {
                    httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
                    httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://ecommercelaps.com"); 
                    
                    httpClient.DefaultRequestHeaders.Remove("X-Title");
                    httpClient.DefaultRequestHeaders.Add("X-Title", "EcommerceLaptop");
                }
                
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
                 if (!string.IsNullOrEmpty(config.BaseUrl))
                 {
                     try 
                     {
                        httpClient.BaseAddress = new System.Uri(config.BaseUrl); 
                     }
                     catch {}
                 }

                 builder.AddOpenAIChatCompletion(
                    modelId: config.ModelId ?? "llama3",
                    apiKey: "dummy",
                    httpClient: httpClient
                );
            }
            else // Default OpenAI
            {
                 if (!string.IsNullOrEmpty(config.BaseUrl))
                 {
                     try 
                     {
                        // Explicitly pass endpoint AND httpClient
                        builder.AddOpenAIChatCompletion(
                            modelId: config.ModelId ?? "gpt-4o",
                            apiKey: config.ApiKey,
                            endpoint: new System.Uri(config.BaseUrl),
                            httpClient: httpClient
                        );
                     }
                     catch 
                     {
                         // Fallback if parsing fails or overload missing (though we expect it to exist)
                         builder.AddOpenAIChatCompletion(
                            modelId: config.ModelId ?? "gpt-4o",
                            apiKey: config.ApiKey,
                            httpClient: httpClient
                        );
                     }
                 }
                 else
                 {
                     builder.AddOpenAIChatCompletion(
                        modelId: config.ModelId ?? "gpt-4o",
                        apiKey: config.ApiKey,
                        httpClient: httpClient
                    );
                 }
            }

            return builder.Build();
        }
    }

    private static string GenerateSlugFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "product";

        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("@", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("^", "")
            .Replace("*", "")
            .Replace("+", "")
            .Replace("=", "")
            .Replace("|", "")
            .Replace("~", "")
            .Replace("`", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("\t", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Replace("--", "-")
            .Replace("---", "-")
            .Trim('-');
    }
}
