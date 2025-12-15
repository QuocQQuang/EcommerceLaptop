using System.Diagnostics.Metrics;
using EcommerceLaptop.Core.Interfaces;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class RagMetricsService : IRagMetricsService
    {
        private readonly Meter _meter;
        private readonly Counter<int> _tokenUsageCounter;
        private readonly Histogram<double> _latencyHistogram;
        private readonly Counter<int> _cacheHitCounter;

        public const string MeterName = "EcommerceLaptop.AI";

        public RagMetricsService()
        {
            _meter = new Meter(MeterName, "1.0.0");
            _tokenUsageCounter = _meter.CreateCounter<int>("ai_token_usage", "tokens", "Number of tokens used");
            _latencyHistogram = _meter.CreateHistogram<double>("ai_operation_latency", "ms", "Latency of AI operations");
            _cacheHitCounter = _meter.CreateCounter<int>("ai_cache_hits", "count", "Number of semantic cache hits");
        }

        public void RecordTokenUsage(int promptTokens, int completionTokens)
        {
            _tokenUsageCounter.Add(promptTokens, new KeyValuePair<string, object?>("type", "prompt"));
            _tokenUsageCounter.Add(completionTokens, new KeyValuePair<string, object?>("type", "completion"));
        }

        public void RecordLatency(double durationMs, string operation)
        {
            _latencyHistogram.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
        }

        public void RecordCacheHit(bool isHit)
        {
             _cacheHitCounter.Add(1, new KeyValuePair<string, object?>("hit", isHit));
        }
    }
}
