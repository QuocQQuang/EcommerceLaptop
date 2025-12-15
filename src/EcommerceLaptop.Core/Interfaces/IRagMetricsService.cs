using System.Diagnostics;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IRagMetricsService
    {
        void RecordTokenUsage(int promptTokens, int completionTokens);
        void RecordLatency(double durationMs, string operation);
        void RecordCacheHit(bool isHit);
    }
}
