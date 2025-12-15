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

            // Simple heuristics for speed (optional optimization)
            if (query.Length < 5) return UserIntent.GeneralChat;

            var kernel = await BuildKernelAsync();
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            
            var history = new ChatHistory();
            history.AddSystemMessage(@"You are an Intent Classifier. 
Classify the user's query into one of the following categories:
- ProductSearch: User is looking for products, laptops, specs, prices, or recommendations.
- Support: User is asking about shipping, returns, warranty, or company policies.
- GeneralChat: Greetings, small talk, or questions unrelated to the store.

Respond ONLY with the category name.");
            history.AddUserMessage(query);

            var result = await chat.GetChatMessageContentAsync(history);
            var intentString = result.Content?.Trim();

            if (Enum.TryParse<UserIntent>(intentString, true, out var intent))
            {
                return intent;
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
                  builder.AddOpenAIChatCompletion(
                     modelId: config.ModelId ?? "gpt-3.5-turbo", // Use cheaper model
                     apiKey: config.ApiKey,
                     httpClient: httpClient
                 );
             }

             return builder.Build();
        }
    }
}
