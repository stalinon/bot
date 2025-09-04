using System.Security.Cryptography;
using System.Text;

using Bot.Abstractions.Contracts;
using Bot.Hosting.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Telegram;

/// <summary>
///     Реализация <see cref="IWebAppInitDataValidator" /> для Telegram Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Вычисляет подпись по алгоритму HMAC-SHA256.</item>
///         <item>Проверяет свежесть поля <c>auth_date</c>.</item>
///         <item>Отбрасывает запросы с дублирующимися параметрами.</item>
///     </list>
/// </remarks>
public sealed class WebAppInitDataValidator : IWebAppInitDataValidator
{
    private const string SecretKeyName = "WebAppData";
    private readonly ILogger<WebAppInitDataValidator> _logger;
    private readonly string _token;
    private readonly TimeSpan _ttl;

    /// <summary>
    ///     Создать экземпляр.
    /// </summary>
    public WebAppInitDataValidator(
        IOptions<BotOptions> options,
        IOptions<WebAppOptions> webOptions,
        ILogger<WebAppInitDataValidator> logger)
    {
        _token = options.Value.Token;
        _ttl = TimeSpan.FromSeconds(webOptions.Value.InitDataTtlSeconds);
        _logger = logger;
    }

    /// <inheritdoc />
    public bool TryValidate(string initData, out string? error)
    {
        var logHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(initData))).ToLowerInvariant()[..8];
        var dict = new Dictionary<string, string>();
        foreach (var pair in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2)
            {
                return Fail("missing_field", "неверный формат", out error);
            }

            if (!dict.TryAdd(kv[0], kv[1]))
            {
                return Fail("missing_field", $"дублирующийся параметр {kv[0]}", out error);
            }
        }

        if (!dict.TryGetValue("hash", out var hash))
        {
            return Fail("missing_field", "отсутствует hash", out error);
        }

        dict.Remove("hash");

        if (!dict.TryGetValue("auth_date", out var authDateRaw) || !long.TryParse(authDateRaw, out var authDateVal))
        {
            return Fail("missing_field", "отсутствует auth_date", out error);
        }

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateVal);
        if (DateTimeOffset.UtcNow - authDate > _ttl)
        {
            return Fail("expired", "истёк срок действия auth_date", out error);
        }

        var dataCheckString = string.Join("\n", dict
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={kv.Value}"));

        using var secretHmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKeyName));
        var secret = secretHmac.ComputeHash(Encoding.UTF8.GetBytes(_token));
        using var hmac = new HMACSHA256(secret);
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
        var computedHash = Convert.ToHexString(computed).ToLowerInvariant();
        if (!string.Equals(computedHash, hash, StringComparison.Ordinal))
        {
            return Fail("bad_hash", "подпись не совпадает", out error);
        }

        error = null;
        return true;

        bool Fail(string reason, string message, out string? err)
        {
            _logger.LogWarning("webapp init validation failed {Reason} {InitDataHash}", reason, logHash);
            err = message;
            return false;
        }
    }
}
