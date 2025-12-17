using System.Threading.Tasks;
using EcommerceLaptop.Core.Enums;
using EcommerceLaptop.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class SemanticKernelIntentClassifier : IIntentClassifier
    {
         private readonly ILlmConfigProvider _configProvider;
         private readonly IHttpClientFactory _httpClientFactory;
         private readonly Microsoft.Extensions.Logging.ILogger<SemanticKernelIntentClassifier> _logger;

        public SemanticKernelIntentClassifier(
            ILlmConfigProvider configProvider,
            IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Logging.ILogger<SemanticKernelIntentClassifier> logger)
        {
            _configProvider = configProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<UserIntent> ClassifyIntentAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return UserIntent.GeneralChat;

            // Simple heuristics for speed
            if (query.Length < 5) return UserIntent.GeneralChat;

            var kernel = await BuildKernelAsync();
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            
            var history = new ChatHistory();
            history.AddSystemMessage(@"You are a strict Intent Classifier for an E-commerce store.
Your job is to categorize the user's input into exactly one of seven categories:

1. ProductSearch: User wants to FIND, SEE, SEARCH for, or LIST products (e.g., 'show me gaming laptops', 'laptop under 20 million', 'search for macbook'). Key intent: BROWSING.
2. ProductAdvice: User wants SPECIFIC ADVICE, CONSULTATION, or COMPARISON about products but is NOT explicitly asking for a list yet (e.g., 'should I buy mac or windows?', 'is 8GB ram enough?', 't vn laptop cho sinh vin', 'which one is better for coding?'). Key intent: CONSULTATION/ADVICE.
3. Support: Post-purchase questions (warranty, returns, repairs, store policies, payment helper).
4. OrderStatus: Tracking orders, shipping status.
5. CartManagement: Adding/removing items from cart (e.g., 'add to cart', 'buy this', 'remove item', 'checkout').
6. AccountManagement: Login, register, profile, password, address.
7. GeneralChat: Casual conversation unrelated to buying specific items, or very high-level tech questions not about store products (e.g., 'hello', 'who are you', 'tell me a joke').

Instructions:
- Respond ONLY with the category name (ProductSearch, ProductAdvice, Support, OrderStatus, CartManagement, AccountManagement, or GeneralChat).
- Do not add punctuation or explanation.");
            
            history.AddUserMessage(query);

            var result = await chat.GetChatMessageContentAsync(history);
            var intentString = result.Content?.Trim() ?? "";

            _logger.LogWarning("[IntentClassifier] Raw Output: '{IntentString}'", intentString);

            var intent = ParseIntentFromOutput(intentString);
            _logger.LogWarning("[IntentClassifier] Parsed Intent: {Intent}", intent);

            return intent;
        }

        public static UserIntent ParseIntentFromOutput(string intentString)
        {
            // 1. Try exact match
            if (Enum.TryParse<UserIntent>(intentString, true, out var intent))
            {
                return intent;
            }

            // 2. Try Regex Fallback (in case of "The intent is ProductSearch.")
            var distinctIntents = Enum.GetNames(typeof(UserIntent));
            foreach (var name in distinctIntents)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(intentString, $@"\b{name}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (Enum.TryParse<UserIntent>(name, true, out var fallbackIntent))
                    {
                        return fallbackIntent;
                    }
                }
            }

            // Fallback
            return UserIntent.GeneralChat;
        }

        private async Task<Kernel> BuildKernelAsync()
        {
             var config = await _configProvider.GetConfigAsync();
             var builder = Kernel.CreateBuilder();
             var httpClient = _httpClientFactory.CreateClient("llm-client"); // Use resilient client

             if (config.Provider == "Azure")
             {
                 builder.AddAzureOpenAIChatCompletion(
                     deploymentName: config.ModelId ?? "gpt-3.5-turbo", // Use cheaper model for classification!
                     endpoint: config.BaseUrl!,
                     apiKey: config.ApiKey,
                     httpClient: httpClient
                 );
             }
             else if (config.Provider == "Ollama")
             {
                  // ... Similar setup to RagChatService ...
                  // For classification we might want a smaller model like phi-3
                  builder.AddOpenAIChatCompletion(
                     modelId: config.ModelId ?? "llama3", 
                     apiKey: "dummy",
                     httpClient: httpClient
                 );
             }
             else 
             {
                 if (!string.IsNullOrEmpty(config.BaseUrl))
                 {
                      builder.AddOpenAIChatCompletion(
                         modelId: config.ModelId ?? "gpt-3.5-turbo",
                         apiKey: config.ApiKey,
                         endpoint: new System.Uri(config.BaseUrl),
                         httpClient: httpClient
                     );
                 }
                 else 
                 {
                      builder.AddOpenAIChatCompletion(
                         modelId: config.ModelId ?? "gpt-3.5-turbo",
                         apiKey: config.ApiKey,
                         httpClient: httpClient
                     );
                 }
             }

             return builder.Build();
        }
    }
}
