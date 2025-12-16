using System.Threading.Tasks;
using EcommerceLaptop.Core.Enums;
using EcommerceLaptop.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net.Http;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class SemanticKernelIntentClassifier : IIntentClassifier
    {
         private readonly ILlmConfigProvider _configProvider;
         private readonly IHttpClientFactory _httpClientFactory;

        public SemanticKernelIntentClassifier(
            ILlmConfigProvider configProvider,
            IHttpClientFactory httpClientFactory)
        {
            _configProvider = configProvider;
            _httpClientFactory = httpClientFactory;
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
Your job is to categorize the user's input into exactly one of three categories:
1. ProductSearch
2. Support
3. GeneralChat

Definitions:
- ProductSearch: User wants to buy, find, or know about products (laptops, gears), specifications, prices, availability, or comparisons.
- Support: User has questions about shipping, returns, warranty, payment, account, or store policies.
- GeneralChat: Greetings, small talk, jokes, or off-topic queries.

Examples:
User: ""I need a gaming laptop under $2000""
Intent: ProductSearch

User: ""How do I return a broken item?""
Intent: Support

User: ""Hello there""
Intent: GeneralChat

User: ""Show me something with 32GB RAM""
Intent: ProductSearch

Instructions:
- Respond ONLY with the category name (ProductSearch, Support, or GeneralChat).
- Do not add punctuation or explanation.");
            
            history.AddUserMessage(query);

            var result = await chat.GetChatMessageContentAsync(history);
            var intentString = result.Content?.Trim() ?? "";

            Console.WriteLine($"[IntentClassifier] Raw Output: '{intentString}'");

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
                        Console.WriteLine($"[IntentClassifier] Regex matched: {fallbackIntent}");
                        return fallbackIntent;
                    }
                }
            }

            // Fallback
            Console.WriteLine("[IntentClassifier] Failed to parse. Defaulting to GeneralChat.");
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
