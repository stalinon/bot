using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Examples.WebAppDemo;

namespace Stalinon.Bot.Examples.WebAppDemo.Tests.Integration;

/// <summary>
///     Фабрика тестового приложения WebAppDemo.
/// </summary>
public sealed class WebAppDemoFactory : WebApplicationFactory<ProgramHost>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var path = Path.Combine(root, "examples", "Stalinon.Bot.Examples.WebAppDemo");
        builder.UseContentRoot(path);

        builder.ConfigureAppConfiguration(cfg =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Bot:Token"] = "000:FAKE",
                ["WebAppAuth:Secret"] = "0123456789ABCDEF0123456789ABCDEF",
                ["WebAppAuth:Lifetime"] = "00:05:00"
            };
            cfg.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<WebAppStatsCollector>();
            services.AddSingleton<IWebAppInitDataValidator>(new StubValidator());
        });
    }

    private sealed class StubValidator : IWebAppInitDataValidator
    {
        public bool TryValidate(string initData, out string? error)
        {
            error = null;
            return true;
        }
    }
}
