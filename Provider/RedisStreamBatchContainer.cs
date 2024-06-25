using Orleans.Runtime;
using Orleans.Streams;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Provider
{
    public class RedisStreamBatchContainer : IBatchContainer
    {
        public StreamId StreamId { get; }

        public StreamSequenceToken SequenceToken { get; }
        public StreamEntry StreamEntry { get; }
        public RedisStreamBatchContainer(StreamEntry streamEntry)
        {
            StreamEntry = streamEntry;
            var streamNamespace = StreamEntry.Values[0].Value;
            var steamKey = StreamEntry.Values[1].Value;
            StreamId = StreamId.Create(streamNamespace!, steamKey!);
            SequenceToken = new RedisStreamSequenceToken(StreamEntry.Id);
        }
        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            List<Tuple<T, StreamSequenceToken>> events = new();
            var eventType = typeof(T).Name;
            if (eventType == StreamEntry.Values[2].Value)
            {
                var data = StreamEntry.Values[3].Value;
                var @event = JsonSerializer.Deserialize<T>(data!);
                events.Add(new(@event!, SequenceToken));
            }
            return events;
        }

        public bool ImportRequestContext()
        {
            return false;
        }
    }
}
