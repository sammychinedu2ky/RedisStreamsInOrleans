using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Runtime;
using Provider;
using StackExchange.Redis;

using IHost host = new HostBuilder()
     .UseOrleansClient(clientBuilder =>
     {
         clientBuilder.Services.AddSingleton<IDatabase>(sp =>
         {
             IDatabase db = ConnectionMultiplexer.Connect("localhost").GetDatabase();
             return db;
         });
         clientBuilder.UseLocalhostClustering();

         clientBuilder.AddPersistentStreams("RedisStream", RedisStreamFactory.Create, null);
         clientBuilder.ConfigureServices(services =>
         {
             services.AddOptions<HashRingStreamQueueMapperOptions>("RedisStream")
                 .Configure(options =>
                 {
                     options.TotalQueueCount = 8;
                 });

         });
     })
     .Build();

await host.StartAsync();

IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
var streamProvider = client.GetStreamProvider("RedisStream");

var streamId = StreamId.Create("numbergenerator", "consecutive");
var stream = streamProvider.GetStream<int>(streamId);

var task = Task.Run(async () =>
{
    var num = 0;
    while (true)
    {
        logger.LogInformation("Sending number {Number}", num);
        Console.WriteLine("Sending number {0}", num);
        //await stream.OnNextAsync(num++);
        await stream.OnCompletedAsync();
        if (num == 20)
        {
            break;
        }
        await Task.Delay(1000);
    }
    await stream.OnCompletedAsync();
});

var streamId2 = StreamId.Create("stringgenerator", "random");
var stream2 = streamProvider.GetStream<string>(streamId2);
List<string> strings = new() { "one", "two", "three", "four", "five" };

var task2 = Task.Run(async () =>
{
    var num = 0;
    while (true)
    {
        var str = strings[num % strings.Count];
        logger.LogInformation("Sending string {String}", str);
        Console.WriteLine("Sending string {0}", str);
       await stream2.OnNextAsync(str);
        num++;
        if (num == 10)
        {
            break;
        }

    }
    await stream2.OnCompletedAsync();
    Console.WriteLine("Stream completed");
});
Console.ReadLine();