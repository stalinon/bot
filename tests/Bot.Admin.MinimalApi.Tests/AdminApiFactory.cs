using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.IO;

namespace Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Фабрика тестового приложения административного API.
/// </summary>
public sealed class AdminApiFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var path = Path.Combine(root, "tests", "Bot.Admin.MinimalApi.Tests");
        builder.UseContentRoot(path);
    }
}

