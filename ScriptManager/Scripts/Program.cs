using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScriptManager.Extensions;
using ScriptManager.Services;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        Manager.SetProgramName("ScriptManager", hostContext.Configuration);

        services.AddHostedService<Manager>();

        services.AddScripts();
    });

//IHost host = hostBuilder.Build();
await hostBuilder.RunConsoleAsync();

Console.WriteLine("Hello world");