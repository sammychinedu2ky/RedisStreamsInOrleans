using Provider;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace RedisStreamsProvider.UnitTests
{
    public class RedisStreamBatchContainerTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var streamEntry = new StreamEntry("1-0", new NameValueEntry[]
            {
                        new NameValueEntry("namespace", "testNamespace"),
                        new NameValueEntry("key", "testKey"),
                        new NameValueEntry("type", "TestEvent"),
                        new NameValueEntry("data", JsonSerializer.Serialize(new TestEvent { Id = 1, Name = "Test" }))
            });

            // Act
            var container = new RedisStreamBatchContainer(streamEntry);

            // Assert
            Assert.Equal("testNamespace", container.StreamId.GetNamespace());
            Assert.Equal("testKey", container.StreamId.GetKeyAsString());
            Assert.Equal(streamEntry.Id.ToString().Split('-').First(), container.SequenceToken.SequenceNumber.ToString());
        }

        [Fact]
        public void GetEvents_ShouldReturnDeserializedEvents()
        {
            // Arrange
            var streamEntry = new StreamEntry("1-0", new NameValueEntry[]
            {
                        new NameValueEntry("namespace", "testNamespace"),
                        new NameValueEntry("key", "testKey"),
                        new NameValueEntry("type", "TestEvent"),
                        new NameValueEntry("data", JsonSerializer.Serialize(new TestEvent { Id = 1, Name = "Test" }))
            });
            var container = new RedisStreamBatchContainer(streamEntry);

            // Act
            var events = container.GetEvents<TestEvent>().ToList();

            // Assert
            Assert.Single(events);
            Assert.Equal(1, events[0].Item1.Id);
            Assert.Equal("Test", events[0].Item1.Name);
            Assert.Equal(streamEntry.Id.ToString().Split('-').First(), events[0].Item2.SequenceNumber.ToString());
        }

        [Fact]
        public void ImportRequestContext_ShouldReturnFalse()
        {
            // Arrange
            var streamEntry = new StreamEntry("1-0", new NameValueEntry[]
            {
                        new NameValueEntry("namespace", "testNamespace"),
                        new NameValueEntry("key", "testKey"),
                        new NameValueEntry("type", "TestEvent"),
                        new NameValueEntry("data", JsonSerializer.Serialize(new TestEvent { Id = 1, Name = "Test" }))
            });
            var container = new RedisStreamBatchContainer(streamEntry);

            // Act
            var result = container.ImportRequestContext();

            // Assert
            Assert.False(result);
        }

        private class TestEvent
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
