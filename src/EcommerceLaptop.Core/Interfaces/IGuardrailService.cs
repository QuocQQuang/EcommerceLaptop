using System.Threading.Tasks;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IGuardrailService
    {
        Task<(bool IsSafe, string Reason)> ValidateInputAsync(string input);
        Task<(bool IsSafe, string Reason)> ValidateOutputAsync(string output);
    }
}
