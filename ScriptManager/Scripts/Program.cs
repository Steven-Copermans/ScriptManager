using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScriptManager.Extensions;
using ScriptManager.Services;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configurationBuilder =>
    {
        //configurationBuilder.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Manager>();

        services.AddScripts();
    });

//IHost host = hostBuilder.Build();
await hostBuilder.RunConsoleAsync();

Console.WriteLine("Hello world");