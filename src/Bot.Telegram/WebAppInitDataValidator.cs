using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Bot.Abstractions.Contracts;
using Bot.Hosting.Options;

using Microsoft.Extensions.Options;

namespace Bot.Telegram;

/// <summary>
///     Реализация <see cref="IWebAppInitDataValidator"/> для Telegram Web App.
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
    private readonly TimeSpan _ttl;
    private readonly string _token;

    /// <summary>
    ///     Создать экземпляр.
    /// </summary>
    public WebAppInitDataValidator(IOptions<BotOptions> options, IOptions<WebAppOptions> webOptions)
    {
        _token = options.Value.Token;
        _ttl = TimeSpan.FromSeconds(webOptions.Value.InitDataTtlSeconds);
    }

    /// <inheritdoc />
    public bool TryValidate(string initData, out string? error)
    {
        var dict = new Dictionary<string, string>();
        foreach (var pair in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2)
            {
                error = "неверный формат";
                return false;
            }

            if (!dict.TryAdd(kv[0], kv[1]))
            {
                error = $"дублирующийся параметр {kv[0]}";
                return false;
            }
        }

        if (!dict.TryGetValue("hash", out var hash))
        {
            error = "отсутствует hash";
            return false;
        }
        dict.Remove("hash");

        if (!dict.TryGetValue("auth_date", out var authDateRaw) || !long.TryParse(authDateRaw, out var authDateVal))
        {
            error = "отсутствует auth_date";
            return false;
        }
        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateVal);
        if (DateTimeOffset.UtcNow - authDate > _ttl)
        {
            error = "истёк срок действия auth_date";
            return false;
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
            error = "подпись не совпадает";
            return false;
        }

        error = null;
        return true;
    }
}
