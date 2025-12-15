using System.Threading.Tasks;
using EcommerceLaptop.Core.Enums;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IIntentClassifier
    {
        Task<UserIntent> ClassifyIntentAsync(string query);
    }
}
