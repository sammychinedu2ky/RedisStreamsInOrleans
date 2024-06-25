using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Provider
{
    public class RedisStreamBatchContainer : IBatchContainer
    {
        public StreamId StreamId => throw new NotImplementedException();
        private rea
        public StreamSequenceToken SequenceToken => throw new NotImplementedException();

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            throw new NotImplementedException();
        }

        public bool ImportRequestContext()
        {
            throw new NotImplementedException();
        }
    }
}
