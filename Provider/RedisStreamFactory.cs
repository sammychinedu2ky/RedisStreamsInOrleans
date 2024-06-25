using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;
using StackExchange.Redis;

namespace Provider
{
    public class RedisStreamFactory : IQueueAdapterFactory
    {
        private readonly IDatabase _database;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _providerName;
        private readonly IStreamFailureHandler _streamFailureHandler;
        private readonly SimpleQueueCacheOptions _simpleQueueCacheOptions;
        private readonly HashRingBasedStreamQueueMapper _hashRingBasedStreamQueueMapper;

        public RedisStreamFactory(IDatabase database,
            ILoggerFactory loggerFactory,
            string providerName,
            IStreamFailureHandler streamFailureHandler,
            SimpleQueueCacheOptions simpleQueueCacheOptions,
            HashRingStreamQueueMapperOptions hashRingStreamQueueMapperOptions
            )
        {
            _database = database;
            _loggerFactory = loggerFactory;
            _providerName = providerName;
            _streamFailureHandler = streamFailureHandler;
            _simpleQueueCacheOptions = simpleQueueCacheOptions;
            _hashRingBasedStreamQueueMapper = new HashRingBasedStreamQueueMapper(hashRingStreamQueueMapperOptions, providerName);
        }
        public Task<IQueueAdapter> CreateAdapter()
        {
            return Task.FromResult<IQueueAdapter>(new RedisStreamAdapter(_database, _providerName, _hashRingBasedStreamQueueMapper, _loggerFactory);
        }

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return Task.FromResult(_streamFailureHandler);
        }

        public IQueueAdapterCache GetQueueAdapterCache()
        {
            return new SimpleQueueAdapterCache(_simpleQueueCacheOptions,_providerName, _loggerFactory);
        }

        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return _hashRingBasedStreamQueueMapper;
        }
    }
}
