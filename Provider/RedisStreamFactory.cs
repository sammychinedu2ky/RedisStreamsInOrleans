﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;
using StackExchange.Redis;

namespace Provider
{
    public class RedisStreamFactory : IQueueAdapterFactory
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _providerName;
        private readonly IStreamFailureHandler _streamFailureHandler;
        private readonly SimpleQueueCacheOptions _simpleQueueCacheOptions;
        private readonly HashRingBasedStreamQueueMapper _hashRingBasedStreamQueueMapper;

        public RedisStreamFactory(IConnectionMultiplexer connectionMultiplexer,
            ILoggerFactory loggerFactory,
            string providerName,
            IStreamFailureHandler streamFailureHandler,
            SimpleQueueCacheOptions simpleQueueCacheOptions,
            HashRingStreamQueueMapperOptions hashRingStreamQueueMapperOptions
            )
        {
            _connectionMultiplexer = connectionMultiplexer;
            _loggerFactory = loggerFactory;
            _providerName = providerName;
            _streamFailureHandler = streamFailureHandler;
            _simpleQueueCacheOptions = simpleQueueCacheOptions;
            _hashRingBasedStreamQueueMapper = new HashRingBasedStreamQueueMapper(hashRingStreamQueueMapperOptions, providerName);
        }

        public static IQueueAdapterFactory Create(IServiceProvider provider, string providerName)
        {
            var connMuliplexer = provider.GetRequiredService<IConnectionMultiplexer>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var simpleQueueCacheOptions = provider.GetOptionsByName<SimpleQueueCacheOptions>(providerName);
            var hashRingStreamQueueMapperOptions = provider.GetOptionsByName<HashRingStreamQueueMapperOptions>(providerName);
            var streamFailureHandler = new RedisStreamFailureHandler(loggerFactory.CreateLogger<RedisStreamFailureHandler>());
            return new RedisStreamFactory(connMuliplexer, loggerFactory, providerName, streamFailureHandler, simpleQueueCacheOptions, hashRingStreamQueueMapperOptions);

        }

        public Task<IQueueAdapter> CreateAdapter()
        {
            return Task.FromResult<IQueueAdapter>(new RedisStreamAdapter(_connectionMultiplexer.GetDatabase(), _providerName, _hashRingBasedStreamQueueMapper, _loggerFactory));
        }

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return Task.FromResult(_streamFailureHandler);
        }

        public IQueueAdapterCache GetQueueAdapterCache()
        {
            return new SimpleQueueAdapterCache(_simpleQueueCacheOptions, _providerName, _loggerFactory);
        }

        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return _hashRingBasedStreamQueueMapper;
        }
    }
}
