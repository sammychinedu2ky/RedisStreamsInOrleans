using Orleans;
using Orleans.Streams;
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

        public override int CompareTo(StreamSequenceToken other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(StreamSequenceToken other)
        {
            throw new NotImplementedException();
        }
    }
}
