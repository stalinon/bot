using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Bot.Abstractions.Contracts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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
        endpoints.MapPost("/webapp/auth", (
            AuthRequest body,
            IWebAppInitDataValidator validator,
            IOptions<WebAppAuthOptions> options,
            HttpRequest req,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("WebAppAuth");
            var sw = Stopwatch.StartNew();
            long userId = 0;

            IResult LogAndReturn(IResult result)
            {
                logger.LogInformation(
                    "webapp auth",
                    new Dictionary<string, object?>
                    {
                        ["webapp_user_id"] = userId,
                        ["source"] = "miniapp",
                        ["latency"] = sw.ElapsedMilliseconds
                    });
                return result;
            }

            if (!req.IsHttps)
            {
                return LogAndReturn(Results.StatusCode(StatusCodes.Status400BadRequest));
            }

            var initData = body.InitData;

            if (!validator.TryValidate(initData, out _))
            {
                return LogAndReturn(Results.StatusCode(StatusCodes.Status401Unauthorized));
            }

            var dict = initData.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);

            if (!dict.TryGetValue("user", out var userRaw) ||
                !dict.TryGetValue("auth_date", out var authDate))
            {
                return LogAndReturn(Results.StatusCode(StatusCodes.Status400BadRequest));
            }

            var userJson = Uri.UnescapeDataString(userRaw);
            var user = JsonSerializer.Deserialize<UserPayload>(userJson, JsonOptions);
            if (user is null)
            {
                return LogAndReturn(Results.StatusCode(StatusCodes.Status400BadRequest));
            }

            userId = user.Id;
            var now = DateTimeOffset.UtcNow;
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("username", user.Username ?? string.Empty),
                new Claim("language_code", user.LanguageCode ?? string.Empty),
                new Claim("auth_date", authDate)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: now.Add(options.Value.Lifetime).UtcDateTime,
                signingCredentials: creds);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return LogAndReturn(Results.Json(new { token = jwt }));
        });

        return endpoints;
    }

    /// <summary>
    ///     Подключить эндпоинт профиля Web App.
    /// </summary>
    public static IEndpointRouteBuilder MapWebAppMe(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/webapp/me", (
            IOptions<WebAppAuthOptions> options,
            HttpRequest req,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("WebAppMe");
            var sw = Stopwatch.StartNew();
            long userId = 0;

            IResult LogAndReturn(IResult result)
            {
                logger.LogInformation(
                    "webapp me",
                    new Dictionary<string, object?>
                    {
                        ["webapp_user_id"] = userId,
                        ["source"] = "miniapp",
                        ["latency"] = sw.ElapsedMilliseconds
                    });
                return result;
            }

            var auth = req.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(auth) ||
                !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return LogAndReturn(Results.StatusCode(StatusCodes.Status401Unauthorized));
            }

            var tokenRaw = auth["Bearer ".Length..].Trim();
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = handler.ValidateToken(tokenRaw, parameters, out _);
                var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var username = principal.FindFirst("username")?.Value;
                var languageCode = principal.FindFirst("language_code")?.Value;
                var authDate = principal.FindFirst("auth_date")?.Value;

                if (sub is null || authDate is null)
                {
                    return LogAndReturn(Results.StatusCode(StatusCodes.Status401Unauthorized));
                }

                userId = long.Parse(sub);
                return LogAndReturn(Results.Json(new
                {
                    sub = userId,
                    username,
                    language_code = languageCode,
                    auth_date = long.Parse(authDate)
                }));
            }
            catch
            {
                return LogAndReturn(Results.StatusCode(StatusCodes.Status401Unauthorized));
            }
        });

        return endpoints;
    }

    private sealed record UserPayload(long Id, string? Username, [property: JsonPropertyName("language_code")] string? LanguageCode);

    private sealed record AuthRequest(string InitData);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
