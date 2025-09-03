using Bot.Admin.MinimalApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdminApi(builder.Configuration);

var app = builder.Build();
app.MapAdminApi();
app.Run();

namespace Bot.Admin.MinimalApi.Tests
{
    /// <summary>
    ///     Точка входа для тестового хоста административного API.
    /// </summary>
    public partial class Program
    {
    }
}

