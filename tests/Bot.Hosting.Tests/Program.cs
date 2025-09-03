using System;
using System.Threading.Channels;
using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Hosting;
using Bot.Hosting.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Hosting.Tests;

/// <summary>
///     Тестовый хост для проб готовности.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Настраивает окружение.</item>
///         <item>Запускает MapHealth.</item>
///     </list>
/// </remarks>
public class Program
{
    /// <summary>
    ///     Точка входа тестового приложения.
    /// </summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var channel = Channel.CreateBounded<UpdateContext>(16);
        var hosted = new BotHostedService(
            new DummyUpdateSource(),
            new DummyPipeline(),
            Array.Empty<Action<IUpdatePipeline>>(),
            new LoggerFactory().CreateLogger<BotHostedService>(),
            Microsoft.Extensions.Options.Options.Create(new BotOptions { Parallelism = 1 }));
        typeof(BotHostedService).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(hosted, channel);

        builder.Services.AddSingleton<IUpdateSource, DummyUpdateSource>();
        builder.Services.AddSingleton<IStateStorage, DummyStateStorage>();
        builder.Services.AddSingleton(hosted);
        builder.Services.AddSingleton(channel);
        builder.Services.AddOptions<BotOptions>().Configure(o => o.Parallelism = 1);

        var app = builder.Build();
        app.MapHealth();
        app.Run();
    }
}

