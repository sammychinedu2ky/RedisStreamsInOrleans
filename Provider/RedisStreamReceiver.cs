using Microsoft.Extensions.Logging;
using Orleans.Streams;
using StackExchange.Redis;


namespace Provider
{
    internal class RedisStreamReceiver : IQueueAdapterReceiver
    {
        private readonly QueueId _queueId;
        private readonly IDatabase _database;
        private readonly ILogger<RedisStreamReceiver> _logger;
        private bool _checkBacklog = true;
        private string _lastId = "0";
        private Task? pendingTasks;

        public RedisStreamReceiver(QueueId queueId, IDatabase database, ILogger<RedisStreamReceiver> logger)
        {
            _queueId = queueId;
            _database = database;
            _logger = logger;
        }

        public async Task<IList<IBatchContainer>?> GetQueueMessagesAsync(int maxCount)
        {
            try
            {
                var events = _database.StreamReadGroupAsync(_queueId.ToString(), "consumer", _queueId.ToString(), _lastId, maxCount);
                pendingTasks = events;
                _lastId = ">";
                var batches = (await events).Select(e => new RedisStreamBatchContainer(e)).ToList<IBatchContainer>();
                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from stream {QueueId}", _queueId);
                return default;
            }
            finally
            {
                pendingTasks = null;
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
                        var ack = _database.StreamAcknowledgeAsync(_queueId.ToString(), "consumer", container.StreamEntry.Id);
                        pendingTasks = ack;
                        await ack;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging messages in stream {QueueId}", _queueId);
            }
            finally
            {
                pendingTasks = null;
            }
        }

        public async Task Shutdown(TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {

                if (pendingTasks is not null)
                {
                    await pendingTasks.WaitAsync(timeout, cts.Token);
                }
            }
            _logger.LogInformation("Shutting down stream {QueueId}", _queueId);
        }
    }
}
