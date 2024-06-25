

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        silo.AddMemoryGrainStorageAsDefault();
    }).UseConsoleLifetime();

using IHost host = builder.Build();
await host.RunAsync();