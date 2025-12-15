using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Interfaces;

namespace EcommerceLaptop.Infrastructure.Services.Security
{
    public class GuardrailService : IGuardrailService
    {
        // Simple keywords for demonstration. In production, use a dedicated AI model or extensive regex.
        private readonly List<string> _unsafeInputKeywords = new() 
        { 
            "ignore previous instructions", 
            "system prompt", 
            "drop table", 
            "exec xp_" 
        };

        private readonly List<string> _unsafeOutputKeywords = new() 
        { 
            "sk-proj-", // OpenAI Key prefix
            "connection string",
            "password"
        };

        public Task<(bool IsSafe, string Reason)> ValidateInputAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Task.FromResult((true, string.Empty));

            var lowerInput = input.ToLowerInvariant();
            foreach (var keyword in _unsafeInputKeywords)
            {
                if (lowerInput.Contains(keyword))
                {
                    return Task.FromResult((false, $"Input contains unsafe keyword: {keyword}"));
                }
            }

            return Task.FromResult((true, string.Empty));
        }

        public Task<(bool IsSafe, string Reason)> ValidateOutputAsync(string output)
        {
             if (string.IsNullOrWhiteSpace(output))
                return Task.FromResult((true, string.Empty));

            // Check for potential secrets leakage
             foreach (var keyword in _unsafeOutputKeywords)
            {
                if (output.Contains(keyword)) // Case sensitive for some secrets, but generally safer to be loose
                {
                     return Task.FromResult((false, "Output contains potential sensitive information."));
                }
            }

            return Task.FromResult((true, string.Empty));
        }
    }
}
