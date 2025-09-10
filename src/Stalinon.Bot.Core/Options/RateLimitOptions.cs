using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stalinon.Bot.Core.Options;

/// <summary>
///     Опции ограничения ддоса
/// </summary>
public sealed class RateLimitOptions : IValidatableObject
{
    /// <summary>
    ///     Кол-во запросов пользователя в минуту
    /// </summary>
    public int PerUserPerMinute { get; set; } = 20;

    /// <summary>
    ///     Кол-во запросов из чата в минуту
    /// </summary>
    public int PerChatPerMinute { get; set; } = 60;

    /// <summary>
    ///     Режим ограничения
    /// </summary>
    public RateLimitMode Mode { get; set; } = RateLimitMode.Hard;

    /// <summary>
    ///     Использовать распределённое хранилище состояний.
    /// </summary>
    public bool UseStateStore { get; set; }
        = false;

    /// <summary>
    ///     Окно времени для подсчёта запросов.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Проверить корректность настроек.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (PerUserPerMinute <= 0)
        {
            yield return new ValidationResult("Лимит пользователя должен быть положительным", [nameof(PerUserPerMinute)]);
        }

        if (PerChatPerMinute <= 0)
        {
            yield return new ValidationResult("Лимит чата должен быть положительным", [nameof(PerChatPerMinute)]);
        }

        if (Window <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Окно должно быть положительным", [nameof(Window)]);
        }

        if (!Enum.IsDefined(typeof(RateLimitMode), Mode))
        {
            yield return new ValidationResult("Некорректный режим", [nameof(Mode)]);
        }
    }
}
