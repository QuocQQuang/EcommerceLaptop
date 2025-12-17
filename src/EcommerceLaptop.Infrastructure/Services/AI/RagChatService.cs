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
        private readonly IOrderService _orderService;
        private readonly IChatPersistenceService _persistenceService;
        private readonly IToolRegistry _toolRegistry;
        private readonly Infrastructure.Services.AI.Plugins.RagChatToolsPlugin _toolsPlugin;
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
            IOrderService orderService,
            IChatPersistenceService persistenceService,
            IToolRegistry toolRegistry,
            Infrastructure.Services.AI.Plugins.RagChatToolsPlugin toolsPlugin,
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
            _orderService = orderService;
            _persistenceService = persistenceService;
            _toolRegistry = toolRegistry;
            _toolsPlugin = toolsPlugin;
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

            // Ensure tools have correct context (SignalR workaround)
            _toolsPlugin.SetContext(userId, sessionId);

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

            // 1. Guardrails (Already passed)


            // 2. Intent Classification
            yield return new ProgressEvent { Stage = "Intent", Message = "Understanding query...", Progress = 0.3 };
            var intent = await _intentClassifier.ClassifyIntentAsync(query);
            
            // Smart Cache: Only cache/retrieve for static intents (GeneralChat, Support)
            // Skip cache for ProductSearch (Real-time Inventory) and Transactional Tools
            if (intent == Core.Enums.UserIntent.GeneralChat || intent == Core.Enums.UserIntent.Support)
            {
                 var cachedResponse = await _cacheService.GetCachedResponseAsync(query);
                 if (cachedResponse != null)
                 {
                     _metricsService.RecordCacheHit(true);
                     yield return new MetadataEvent 
                     { 
                         ProcessingStage = "Cache", 
                         CacheHit = true, 
                         ElapsedMs = stopwatch.ElapsedMilliseconds,
                         SessionId = sessionId 
                     };
                     yield return new TokenEvent { Token = cachedResponse.Content, Index = 0 };
                     
                     await _persistenceService.SaveMessageAsync(sessionId, "user", query);
                     await _persistenceService.SaveMessageAsync(sessionId, "assistant", cachedResponse.Content);
                     
                     yield return new CompleteEvent 
                     { 
                         QueryId = System.Guid.NewGuid().ToString(),
                         TotalTokens = cachedResponse.Content.Length / 4, 
                         DurationMs = stopwatch.ElapsedMilliseconds,
                         CacheHit = true,
                         ModelUsed = "Cache"
                     };
                     yield break;
                 }
            }
            
            // Send Initial Metadata with SessionID to Client
            yield return new MetadataEvent 
            { 
                 ProcessingStage = "Init", 
                 ItemsFound = 0, 
                 CacheHit = false, 
                 ElapsedMs = 0,
                 SessionId = sessionId 
            };
            
            string contextString = "";
            List<string> sources = new();

            // 3. Routing based on Intent
            Console.WriteLine($"[DEBUG] Intent: {intent}");
            if (intent == Core.Enums.UserIntent.ProductSearch || 
                intent == Core.Enums.UserIntent.CartManagement ||
                intent == Core.Enums.UserIntent.ProductAdvice)
            {
                yield return new ProgressEvent { Stage = "Retrieval", Message = "Searching products...", Progress = 0.5 };
                
                List<ProductEvent> foundProducts = new();
                string? retrievalError = null;

                try
                {
                    // STEP 5 & 7: Query Rewriting and Hard Filter Extraction
                    // We need a lightweight Kernel/Service for this pre-processing step
                    var preProcessKernel = await BuildKernelAsync();
                    var chatService = preProcessKernel.GetRequiredService<IChatCompletionService>();
                    
                    var rewritingHistory = new ChatHistory();
                    rewritingHistory.AddSystemMessage(@"You are a Search Optimizer for a Laptop Store.
Analyze the user's query and extract:
1. 'rewritten_query': A keyword-test optimized English version of the query for Vector Search (e.g., 'laptop gaming' -> 'gaming laptop high performance discrete gpu').
2. 'filters': A dictionary of hard filters if explicitly stated. Supported keys: 'price_min' (double), 'price_max' (double), 'category_id' (int, 4=Gaming, 5=Thin&Light, 6=Business, 7=Apple), 'brand_id' (int).
3. 'sort': 'price_asc' or 'price_desc' if requested.

Output JSON ONLY:
{
  ""rewritten_query"": ""string"",
  ""filters"": { ""price_max"": 20000000, ""category_id"": ""4"" }
}");
                    rewritingHistory.AddUserMessage(query);

                    var rewriteSettings = new OpenAIPromptExecutionSettings { Temperature = 0.1, ResponseFormat = "json_object" }; // Force JSON
                    var rewriteResult = await chatService.GetChatMessageContentAsync(rewritingHistory, rewriteSettings, preProcessKernel);
                    
                    string optimizedQuery = query;
                    Dictionary<string, object>? hardFilters = null;

                    try 
                    {
                        var json = System.Text.Json.JsonDocument.Parse(rewriteResult.Content!);
                        if (json.RootElement.TryGetProperty("rewritten_query", out var rq)) optimizedQuery = rq.GetString() ?? query;
                        
                        if (json.RootElement.TryGetProperty("filters", out var f) && f.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            hardFilters = new Dictionary<string, object>();
                            foreach (var prop in f.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    hardFilters[prop.Name] = prop.Value.GetDouble();
                                else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                    hardFilters[prop.Name] = prop.Value.GetString()!;
                            }
                        }
                        _logger.LogInformation($"[RAG] Rewrote '{query}' to '{optimizedQuery}'. Filters: {System.Text.Json.JsonSerializer.Serialize(hardFilters)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[RAG] Failed to parse rewrite result. Using original query.");
                    }

                    // RAG Flow with Optimized Query and Filters
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(optimizedQuery);
                    var searchResults = await _vectorDbService.SearchAsync(CollectionName, embedding, limit: 10, filter: hardFilters);
                    
                    // Real-time Data Enrichment
                    var productIds = searchResults
                        .Select(r => 
                        {
                            // ... existing ID extraction logic ... (keeping it concise for this replacement)
                            if (r.Metadata.TryGetValue("product_id", out var pidObj))
                            {
                                if (pidObj is int i) return i;
                                if (pidObj is long l) return (int)l;
                                if (pidObj is double d) return (int)d; // Json number
                                if (pidObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number) return je.GetInt32();
                                if (pidObj is string s && int.TryParse(s, out var parsed)) return parsed;
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
                                // Debug logging to file
                                try { System.IO.File.AppendAllText("rag_debug.txt", $"[DEBUG] Result {r.Id} Metadata Keys: {string.Join(", ", r.Metadata.Keys)}\nValue Type: {pidObj?.GetType().Name}\nValue: {pidObj}\n"); } catch {}

                                if (pidObj is int i) pid = i;
                                else if (pidObj is long l) pid = (int)l;
                                else if (pidObj is double d) pid = (int)d;
                                else if (pidObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number) pid = je.GetInt32();
                                else if (pidObj is string s && int.TryParse(s, out var parsed)) pid = parsed;
                            }

                            if (pid.HasValue && productDict.TryGetValue(pid.Value, out var product))
                            {
                                var quantityInStock = product.Inventory?.QuantityInStock ?? 0;
                                var stockStatus = quantityInStock > 0 
                                    ? $"Stock: {quantityInStock} (In Stock)" 
                                    : "Out of Stock";
                                return $"**Product ID: {product.Id}**\nName: {product.Name}\nPrice: ${product.Price}\n{stockStatus}\nDescription: {r.Content}";
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
                    _logger.LogInformation($"[DEBUG] Found {foundProducts.Count} products.");
                    
                    // FIX: Only show visual product cards if the user explicitly WANTED to search.
                    // For CartManagement (e.g. "add this to cart"), we need RAG for context/ID, 
                    // but we shouldn't spam the chat with product cards again.
                    if (intent == Core.Enums.UserIntent.ProductSearch)
                    {
                        foreach (var p in foundProducts)
                        {
                            yield return p;
                        }
                    }
                }
            }
            else if (intent == Core.Enums.UserIntent.Support)
            {
                contextString = "Store Policy: We offer 30-day returns. Warranty is 1 year for all laptops. Shipping is free for orders over $500.";
            }
            else if (intent == Core.Enums.UserIntent.OrderStatus)
            {
                yield return new ProgressEvent { Stage = "OrderRetrieval", Message = "Checking order status...", Progress = 0.5 };
                
                var (events, message) = await GetOrderEventsForQueryAsync(query);
                foreach(var evt in events) yield return evt;
                contextString = message;
            }
            else if (RequiresToolCalling(intent))
            {
                // Tool Calling Path: Enable tools and let SK handle it
                yield return new ProgressEvent { Stage = "Tools", Message = "Analyzing request with tools...", Progress = 0.5 };
                _logger.LogInformation("Tool calling path activated for intent: {Intent}", intent);
                
                // Provide context hint to LLM but do NOT force a hardcoded response
                contextString = $"User Intent: {intent}. Use available tools to satisfy the request if needed.";
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

            var executionSettings = new OpenAIPromptExecutionSettings() 
            { 
                Temperature = 0.7,
                // Disable tools if we already handled it via BFF (OrderStatus) to prevent double data (Markdown + Rich UI)
                ToolCallBehavior = intent == Core.Enums.UserIntent.OrderStatus 
                    ? null 
                    : ToolCallBehavior.AutoInvokeKernelFunctions
            };
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
                     
                     // Smart Cache Save: Only for static intents
                     if (intent == Core.Enums.UserIntent.GeneralChat || intent == Core.Enums.UserIntent.Support)
                     {
                        await _cacheService.CacheResponseAsync(query, fullResponse);
                     }
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
            
            // Register Tools Plugin
            builder.Plugins.AddFromObject(_toolsPlugin, "RagChatTools");

            var httpClient = _httpClientFactory.CreateClient("llm-client");

            // Apply global headers from configuration
            if (config.Headers != null)
            {
                foreach (var header in config.Headers)
                {
                    if (httpClient.DefaultRequestHeaders.Contains(header.Key))
                        httpClient.DefaultRequestHeaders.Remove(header.Key);
                    
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            // Special handling for legacy OpenRouter setup if not in headers
            if (config.BaseUrl?.Contains("openrouter.ai") == true && !httpClient.DefaultRequestHeaders.Contains("HTTP-Referer"))
            {
                 httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://ecommercelaps.com");
                 httpClient.DefaultRequestHeaders.Add("X-Title", "EcommerceLaptop");
            }

            if (config.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: config.ModelId ?? "gpt-4o",
                    endpoint: config.BaseUrl!,
                    apiKey: config.ApiKey,
                    httpClient: httpClient
                );
            }
            else if (config.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                 // Ollama typically runs on localhost endpoint, often without auth
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
                    apiKey: "dummy", // Ollama doesn't typically need a key
                    httpClient: httpClient
                );
            }
            else // Default OpenAI and Compatible Services (DeepSeek, OpenRouter, etc.)
            {
                 // If BaseUrl is provided, configure the client to use it
                 if (!string.IsNullOrEmpty(config.BaseUrl))
                 {
                     try 
                     {
                        // Some SK extensions need the endpoint explicitly passed if it's not default OpenAI
                        builder.AddOpenAIChatCompletion(
                            modelId: config.ModelId ?? "gpt-4o",
                            apiKey: config.ApiKey,
                            endpoint: new System.Uri(config.BaseUrl),
                            httpClient: httpClient
                        );
                     }
                     catch 
                     {
                         // Fallback mechanism
                         builder.AddOpenAIChatCompletion(
                            modelId: config.ModelId ?? "gpt-4o",
                            apiKey: config.ApiKey,
                            httpClient: httpClient
                        );
                     }
                 }
                 else
                 {
                     // Standard OpenAI
                     builder.AddOpenAIChatCompletion(
                        modelId: config.ModelId ?? "gpt-4o",
                        apiKey: config.ApiKey,
                        httpClient: httpClient
                    );
                 }
            }

            return builder.Build();
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

    private async Task<(List<Core.DTOs.Chat.OrderEvent>, string)> GetOrderEventsForQueryAsync(string query)
    {
        var events = new List<Core.DTOs.Chat.OrderEvent>();
        var (effectiveUserIdStr, _) = _toolsPlugin.GetUserContext();
        
        if (string.IsNullOrEmpty(effectiveUserIdStr) || !int.TryParse(effectiveUserIdStr, out var userId))
        {
            return (events, "Please login to view your orders.");
        }

        try
        {
            int? orderId = null;
            var match = System.Text.RegularExpressions.Regex.Match(query, @"#?(\d+)");
            if (match.Success)
            {
               if (int.TryParse(match.Groups[1].Value, out var id)) orderId = id;
            }

            if (orderId.HasValue)
            {
                 var order = await _orderService.GetOrderDetailsAsync(orderId.Value);
                 if (order != null && order.CustomerId == userId)
                 {
                     events.Add(new Core.DTOs.Chat.OrderEvent 
                     {
                         Id = order.Id,
                         Status = order.Status.ToString(),
                         CreatedAt = order.CreatedAt,
                         TotalAmount = order.TotalAmount,
                         ItemCount = order.Items.Count,
                         Items = order.Items.Select(i => $"{i.Quantity}x {i.ProductName}").ToList()
                     });
                     return (events, $"Found order #{order.Id}. It is currently {order.Status}.");
                 }
                 return (events, $"Order #{orderId.Value} not found.");
            }
            else
            {
                 var orders = await _orderService.GetCustomerOrdersAsync(userId, 1, 5);
                 if (orders.Items.Any())
                 {
                     foreach(var o in orders.Items)
                     {
                         events.Add(new Core.DTOs.Chat.OrderEvent 
                         {
                             Id = o.Id,
                             Status = o.Status.ToString(),
                             CreatedAt = o.CreatedAt,
                             TotalAmount = o.TotalAmount,
                             ItemCount = o.Items.Count,
                             // OrderDto might not populate Items in list view, check if needed
                             // Typically list view returns OrderDto which has Items property but it might be empty
                             // Assuming it's populated or we accept empty list for list card
                             Items = o.Items.Select(i => $"{i.Quantity}x {i.ProductName}").ToList()
                         });
                     }
                     return (events, "Here are your recent orders.");
                 }
                 return (events, "You have no orders yet.");
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error in GetOrderEventsForQueryAsync");
             return (events, "I had trouble retrieving your order information.");
        }
    }

    private static bool RequiresToolCalling(Core.Enums.UserIntent intent) => intent switch
    {
        Core.Enums.UserIntent.OrderStatus => true,
        Core.Enums.UserIntent.CartManagement => true,
        Core.Enums.UserIntent.AccountManagement => true,
        _ => false
    };
}
}
