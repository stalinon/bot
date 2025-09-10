using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Stalinon.Bot.WebApp.MinimalApi.Tests;

/// <summary>
///     Фабрика тестового приложения Web App API.
/// </summary>
public sealed class WebAppApiFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var path = Path.Combine(root, "tests", "Stalinon.Bot.WebApp.MinimalApi.Tests");
        builder.UseContentRoot(path);
    }
}
