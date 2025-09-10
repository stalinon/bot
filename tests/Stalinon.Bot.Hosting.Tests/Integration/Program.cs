using System.Reflection;
using System.Threading.Channels;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Hosting.Options;

namespace Stalinon.Bot.Hosting.Tests;

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
            new StatsCollector(),
            new LoggerFactory().CreateLogger<BotHostedService>(),
            Microsoft.Extensions.Options.Options.Create(new BotOptions
            {
                Transport = new TransportOptions { Parallelism = 1 }
            }),
            Microsoft.Extensions.Options.Options.Create(new StopOptions()));
        typeof(BotHostedService).GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(hosted,
            channel);

        builder.Services.AddSingleton<IUpdateSource, DummyUpdateSource>();
        builder.Services.AddSingleton<IStateStore, DummyStateStore>();
        builder.Services.AddSingleton(hosted);
        builder.Services.AddSingleton(channel);
        builder.Services.AddOptions<BotOptions>().Configure(o => o.Transport.Parallelism = 1);
        builder.Services.AddOptions<StopOptions>();

        var app = builder.Build();
        app.MapHealth();
        app.Run();
    }
}
