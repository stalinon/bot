using Microsoft.AspNetCore.Builder;

using Stalinon.Bot.Admin.MinimalApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdminApi(builder.Configuration);

var app = builder.Build();
app.MapAdminApi();
app.Run();

namespace Stalinon.Bot.Admin.MinimalApi.Tests
{
    /// <summary>
    ///     Точка входа для тестового хоста административного API.
    /// </summary>
    public class Program
    {
    }
}
