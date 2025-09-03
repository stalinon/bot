using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Bot.Hosting.Options;
using Bot.Telegram;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Bot.Telegram.Tests;

/// <summary>
///     Тесты валидатора данных Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется успех при валидном наборе.</item>
///         <item>Проверяется ошибка при неверной подписи.</item>
///         <item>Проверяется ошибка при просроченном <c>auth_date</c>.</item>
///         <item>Проверяется ошибка при отсутствии обязательного поля.</item>
///         <item>Проверяется ошибка при дублировании параметров.</item>
///         <item>Проверяется успех при другом порядке параметров.</item>
///     </list>
/// </remarks>
public sealed class WebAppInitDataValidatorTests
{
    private const string Token = "token";

    /// <summary>
    ///     Тест 1: Возвращает успех при валидных данных.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Возвращает успех при валидных данных.")]
    public void Should_ReturnTrue_OnValidData()
    {
        var sut = CreateSut(out var logs);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = now,
            ["user"] = "u"
        });

        var result = sut.TryValidate(data, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
        logs.Logs.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 2: Возвращает ошибку при неверной подписи.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Возвращает ошибку при неверной подписи.")]
    public void Should_ReturnError_OnInvalidSignature()
    {
        var sut = CreateSut(out var logs);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = now,
            ["user"] = "u"
        });
        data = data.Replace("hash=", "hash=deadbeef");

        var result = sut.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("подпись");
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))).ToLowerInvariant()[..8];
        var log = logs.Logs.Should().ContainSingle().Which;
        log.Message.Should().Contain("bad_hash").And.Contain(expectedHash);
    }

    /// <summary>
    ///     Тест 3: Возвращает ошибку при просроченном <c>auth_date</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Возвращает ошибку при просроченном auth_date.")]
    public void Should_ReturnError_OnExpiredAuthDate()
    {
        var sut = CreateSut(out var logs);
        var old = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = old,
            ["user"] = "u"
        });

        var result = sut.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("auth_date");
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))).ToLowerInvariant()[..8];
        var log = logs.Logs.Should().ContainSingle().Which;
        log.Message.Should().Contain("expired").And.Contain(expectedHash);
    }

    /// <summary>
    ///     Тест 4: Возвращает ошибку при отсутствии обязательного поля.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Возвращает ошибку при отсутствии обязательного поля.")]
    public void Should_ReturnError_WhenFieldMissing()
    {
        var sut = CreateSut(out var logs);
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["user"] = "u"
        });

        var result = sut.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("auth_date");
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))).ToLowerInvariant()[..8];
        var log = logs.Logs.Should().ContainSingle().Which;
        log.Message.Should().Contain("missing_field").And.Contain(expectedHash);
    }

    /// <summary>
    ///     Тест 5: Возвращает ошибку при дублировании параметров.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Возвращает ошибку при дублировании параметров.")]
    public void Should_ReturnError_OnDuplicateParameters()
    {
        var sut = CreateSut(out var logs);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = now,
            ["user"] = "u"
        });
        data = $"user=u&{data}";

        var result = sut.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("дублирующийся");
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))).ToLowerInvariant()[..8];
        var log = logs.Logs.Should().ContainSingle().Which;
        log.Message.Should().Contain("missing_field").And.Contain(expectedHash);
    }

    /// <summary>
    ///     Тест 6: Возвращает успех при другом порядке параметров.
    /// </summary>
    [Fact(DisplayName = "Тест 6: Возвращает успех при другом порядке параметров.")]
    public void Should_ReturnTrue_WhenOrderDiffers()
    {
        var sut = CreateSut(out var logs);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["a"] = "1",
            ["auth_date"] = now,
            ["b"] = "2"
        });
        var parts = data.Split('&');
        Array.Reverse(parts);
        data = string.Join('&', parts);

        var result = sut.TryValidate(data, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
        logs.Logs.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 7: Возвращает успех при увеличенном TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 7: Возвращает успех при увеличенном TTL.")]
    public void Should_ReturnTrue_WhenTtlIncreased()
    {
        var opts = Options.Create(new WebAppOptions { InitDataTtlSeconds = 600 });
        var validator = new WebAppInitDataValidator(
            Options.Create(new BotOptions { Token = Token }),
            opts,
            NullLogger<WebAppInitDataValidator>.Instance);
        var old = DateTimeOffset.UtcNow.AddMinutes(-9).ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = old,
            ["user"] = "u"
        });

        var result = validator.TryValidate(data, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    private static string CreateInitData(IDictionary<string, string> fields)
    {
        var dataCheckString = string.Join("\n", fields.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
        using var secretHmac = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
        var secret = secretHmac.ComputeHash(Encoding.UTF8.GetBytes(Token));
        using var hmac = new HMACSHA256(secret);
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString))).ToLowerInvariant();
        var query = string.Join("&", fields.Select(x => $"{x.Key}={x.Value}"));
        return $"{query}&hash={hash}";
    }

    private static WebAppInitDataValidator CreateSut(out CollectingLoggerProvider provider)
    {
        provider = new CollectingLoggerProvider();
        var factory = new LoggerFactory(new[] { provider });
        var logger = factory.CreateLogger<WebAppInitDataValidator>();
        var options = Options.Create(new BotOptions { Token = Token });
        var web = Options.Create(new WebAppOptions());
        return new WebAppInitDataValidator(options, web, logger);
    }
}
