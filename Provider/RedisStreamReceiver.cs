﻿using Microsoft.Extensions.Logging;
using Orleans.Streams;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Provider
{
    internal class RedisStreamReceiver : IQueueAdapterReceiver
    {
        private readonly QueueId _queueId;
        private readonly IDatabase _database;
        private readonly ILogger<RedisStreamReceiver> _logger;
        private bool _checkBacklog = true;
        private string _lastId = "0";

        public RedisStreamReceiver(QueueId queueId, IDatabase database, Microsoft.Extensions.Logging.ILogger<RedisStreamReceiver> logger)
        {
            _queueId = queueId;
            _database = database;
            _logger = logger;
        }

        public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            try
            {
                if (_checkBacklog) maxCount = 0;
                var events = await _database.StreamReadGroupAsync(_queueId.ToString(), "consumer", _queueId.ToString(), _lastId, maxCount);
                _lastId = ">";
                _checkBacklog = false;
                var batches = events.Select(e => new RedisStreamBatchContainer(e)).ToList<IBatchContainer>();
                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from stream {QueueId}", _queueId);
                return default;
            }

        }

        public async Task Initialize(TimeSpan timeout)
        {
            try
            {
                using (var cts = new CancellationTokenSource(timeout))
                {
                    var task = _database.StreamCreateConsumerGroupAsync(_queueId.ToString(), "consumer", "$", true);
                    await task.WaitAsync(timeout, cts.Token);                    
                }
            }
            catch (Exception ex) when (ex.Message.Contains("name already exists")) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing stream {QueueId}", _queueId);
            }
        }

        public async Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    var container = message as RedisStreamBatchContainer;
                    if (container != null)
                    {
                        await _database.StreamAcknowledgeAsync(_queueId.ToString(), "consumer", container.StreamEntry.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging messages in stream {QueueId}", _queueId);
            }
        }

        public Task Shutdown(TimeSpan timeout)
        {
            // implement any shut
        }
    }
}
