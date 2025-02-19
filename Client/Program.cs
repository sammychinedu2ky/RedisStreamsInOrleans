﻿using Microsoft.Extensions.DependencyInjection;
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
     .ConfigureLogging(logging => logging.AddConsole())
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
        await stream.OnNextAsync(num++);

        if (num == 20)
        {
            break;
        }
        await Task.Delay(1000);
    }
});


Console.ReadLine();