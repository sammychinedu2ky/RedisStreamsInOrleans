using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace Provider
{
    public class RedisStreamFailureHandler : IStreamFailureHandler
    {
        private readonly ILogger<RedisStreamFailureHandler> _logger;

        public RedisStreamFailureHandler(ILogger<RedisStreamFailureHandler> logger)
        {
            _logger = logger;
        }

        public bool ShouldFaultSubsriptionOnError => true;


        public Task OnDeliveryFailure(GuidId subscriptionId, string streamProviderName, StreamId streamIdentity, StreamSequenceToken sequenceToken)
        {
            _logger.LogError("Delivery failure for subscription {SubscriptionId} on stream {StreamId} with token {Token}", subscriptionId, streamIdentity, sequenceToken);
            return Task.CompletedTask;
        }

        public Task OnSubscriptionFailure(GuidId subscriptionId, string streamProviderName, StreamId streamIdentity, StreamSequenceToken sequenceToken)
        {
            _logger.LogError("Subscription failure for subscription {SubscriptionId} on stream {StreamId} with token {Token}", subscriptionId, streamIdentity, sequenceToken);
            return Task.CompletedTask;
        }
    }
}
