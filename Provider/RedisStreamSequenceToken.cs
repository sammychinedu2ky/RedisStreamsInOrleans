using Orleans;
using Orleans.Streams;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Provider
{
    [GenerateSerializer]
    internal class RedisStreamSequenceToken : StreamSequenceToken
    {
        [Id(0)]
        public override long SequenceNumber { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }
        [Id(1)]
        public override int EventIndex { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        public RedisStreamSequenceToken(RedisValue id)
        {

            var split = id.ToString().Split("-");
            SequenceNumber = long.Parse(split[0]);
            EventIndex = int.Parse(split[1]);
        }
        public RedisStreamSequenceToken(long sequenceNumber, int eventIndex)
        {
            SequenceNumber = sequenceNumber;
            EventIndex = eventIndex;
        }
        public override int CompareTo(StreamSequenceToken other)
        {
            if(other is null) throw new ArgumentNullException(nameof(other));
            if(other is RedisStreamSequenceToken token)
            {
                if(SequenceNumber == token.SequenceNumber)
                {
                    return EventIndex.CompareTo(token.EventIndex);
                }
                return SequenceNumber.CompareTo(token.SequenceNumber);
            }
            throw new ArgumentException("Invalid token type", nameof(other));
        }

        public override bool Equals(StreamSequenceToken other)
        {
            var token = other as RedisStreamSequenceToken;
            return token != null && SequenceNumber == token.SequenceNumber && EventIndex == token.EventIndex;
        }
    }
}
