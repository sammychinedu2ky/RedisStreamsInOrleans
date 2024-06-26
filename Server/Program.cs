

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Streams;
using Provider;
using StackExchange.Redis;

var builder = new HostBuilder()
    .UseOrleans(silo =>
    {
        silo.UseLocalhostClustering();
        silo.Services.AddSingleton<IDatabase>(sp =>
        {
            return ConnectionMultiplexer.Connect("localhost").GetDatabase();
        });
        silo.ConfigureLogging(logging => logging.AddConsole());
        silo.AddMemoryGrainStorage("PubSubStore");
        silo.AddPersistentStreams("RedisStream", Provider.RedisStreamFactory.Create, null);
        silo.AddMemoryGrainStorageAsDefault();
    }).UseConsoleLifetime();
builder.ConfigureServices(services =>
{
    services.AddOptions<HashRingStreamQueueMapperOptions>("RedisStream")
        .Configure(options =>
        {
            options.TotalQueueCount = 8;
        });
    services.AddOptions<SimpleQueueCacheOptions>("RedisStream");
});
using IHost host = builder.Build();
await host.RunAsync();


[ImplicitStreamSubscription("numbergenerator")]
public class NumberGeneratorGrain : Grain, INumberGeneratorGrain, IAsyncObserver<int>
{
    private ILogger<NumberGeneratorGrain> _logger { get; }

    public NumberGeneratorGrain(ILogger<NumberGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        var streamProvider = this.GetStreamProvider("RedisStream");
        var streamId = StreamId.Create("numbergenerator", this.GetPrimaryKeyString());
       
        _logger.LogInformation("Subscribing to stream {StreamId}", streamId);
        _logger.LogInformation("Grain id is {Id}", this.GetPrimaryKeyString());
        var stream = streamProvider.GetStream<int>(streamId);
        await stream.SubscribeAsync(this);
        await base.OnActivateAsync(ct);
    }
    public Task OnCompletedAsync()
    {
        _logger.LogInformation("Stream completed");
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        _logger.LogError("Error: {Error}", ex.Message);
        return Task.CompletedTask;
    }

    public async Task OnNextAsync(int item, StreamSequenceToken? token = null)
    {
        _logger.LogInformation("Received number {Number}", item);
        await Task.Delay(2000);
       
    }
}

[ImplicitStreamSubscription("stringgenerator")]
public class StringGeneratorGrain : Grain, IStringGeneratorGrain, IAsyncObserver<string>
{
    private ILogger<StringGeneratorGrain> _logger { get; }

    public StringGeneratorGrain(ILogger<StringGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        var streamProvider = this.GetStreamProvider("RedisStream");
        var streamId = StreamId.Create("stringgenerator", this.GetPrimaryKeyString());

        _logger.LogInformation("Subscribing to stream {StreamId}", streamId);
        _logger.LogInformation("Grain id is {Id}", this.GetPrimaryKeyString());
        var stream = streamProvider.GetStream<string>(streamId);
        await stream.SubscribeAsync(this);
        await base.OnActivateAsync(ct);

    }
    public Task OnCompletedAsync()
    {
        _logger.LogInformation("Stream completed");
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        _logger.LogError("Error: {Error}", ex.Message);
        return Task.CompletedTask;
    }

    public async Task OnNextAsync(string item, StreamSequenceToken? token = null)
    {
        _logger.LogInformation("Received string {Number}", item);
        
        await Task.Delay(2000);
        await this.OnCompletedAsync();
    }
}

public interface INumberGeneratorGrain : IGrainWithStringKey { }
public interface IStringGeneratorGrain : IGrainWithStringKey { }