using System.IO;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Bot.Hosting.Tests;

/// <summary>
///     Фабрика тестового хоста.
/// </summary>
public sealed class HostingFactory : WebApplicationFactory<Program>
{
    /// <summary>
    ///     Создать хост с корректным корнем содержимого.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        return base.CreateHost(builder);
    }
}

