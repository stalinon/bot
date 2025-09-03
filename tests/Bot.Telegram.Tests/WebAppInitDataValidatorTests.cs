using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Bot.Hosting.Options;
using Bot.Telegram;

using FluentAssertions;

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
    private readonly WebAppInitDataValidator _validator;

    /// <inheritdoc/>
    public WebAppInitDataValidatorTests()
    {
        _validator = new WebAppInitDataValidator(Options.Create(new BotOptions { Token = Token }));
    }

    /// <summary>
    ///     Тест 1: Возвращает успех при валидных данных.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Возвращает успех при валидных данных.")]
    public void Should_ReturnTrue_OnValidData()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = now,
            ["user"] = "u"
        });

        var result = _validator.TryValidate(data, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    /// <summary>
    ///     Тест 2: Возвращает ошибку при неверной подписи.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Возвращает ошибку при неверной подписи.")]
    public void Should_ReturnError_OnInvalidSignature()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = now,
            ["user"] = "u"
        });
        data = data.Replace("hash=", "hash=deadbeef");

        var result = _validator.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("подпись");
    }

    /// <summary>
    ///     Тест 3: Возвращает ошибку при просроченном <c>auth_date</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Возвращает ошибку при просроченном auth_date.")]
    public void Should_ReturnError_OnExpiredAuthDate()
    {
        var old = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = old,
            ["user"] = "u"
        });

        var result = _validator.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("auth_date");
    }

    /// <summary>
    ///     Тест 4: Возвращает ошибку при отсутствии обязательного поля.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Возвращает ошибку при отсутствии обязательного поля.")]
    public void Should_ReturnError_WhenFieldMissing()
    {
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["user"] = "u"
        });

        var result = _validator.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("auth_date");
    }

    /// <summary>
    ///     Тест 5: Возвращает ошибку при дублировании параметров.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Возвращает ошибку при дублировании параметров.")]
    public void Should_ReturnError_OnDuplicateParameters()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var data = CreateInitData(new Dictionary<string, string>
        {
            ["query_id"] = "1",
            ["auth_date"] = now,
            ["user"] = "u"
        });
        data = $"user=u&{data}";

        var result = _validator.TryValidate(data, out var error);

        result.Should().BeFalse();
        error.Should().Contain("дублирующийся");
    }

    /// <summary>
    ///     Тест 6: Возвращает успех при другом порядке параметров.
    /// </summary>
    [Fact(DisplayName = "Тест 6: Возвращает успех при другом порядке параметров.")]
    public void Should_ReturnTrue_WhenOrderDiffers()
    {
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

        var result = _validator.TryValidate(data, out var error);

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
}
