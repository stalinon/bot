using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bot.Telegram;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bot.WebApp.MinimalApi;

/// <summary>
///     Расширения для эндпоинтов Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет <c>initData</c>.</item>
///         <item>Выдаёт короткий JWT при успехе.</item>
///     </list>
/// </remarks>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Подключить эндпоинт авторизации Web App.
    /// </summary>
    public static IEndpointRouteBuilder MapWebAppAuth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/webapp/auth", (
            string initData,
            IWebAppInitDataValidator validator,
            IOptions<WebAppAuthOptions> options,
            HttpRequest req) =>
        {
            if (!req.IsHttps)
            {
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            if (!validator.TryValidate(initData, out _))
            {
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            var dict = initData.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);

            if (!dict.TryGetValue("user", out var userRaw))
            {
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            var userJson = Uri.UnescapeDataString(userRaw);
            var user = JsonSerializer.Deserialize<UserPayload>(userJson, JsonOptions);
            if (user is null)
            {
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            var now = DateTimeOffset.UtcNow;
            var claims = new[]
            {
                new Claim("user_id", user.Id.ToString()),
                new Claim("username", user.Username ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: now.AddMinutes(5).UtcDateTime,
                signingCredentials: creds);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return Results.Text(jwt);
        });

        return endpoints;
    }

    private sealed record UserPayload(long Id, string? Username);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
