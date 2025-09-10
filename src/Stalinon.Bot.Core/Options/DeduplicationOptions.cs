using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stalinon.Bot.Core.Options;

/// <summary>
///     Опции дедупликации
/// </summary>
public sealed class DeduplicationOptions : IValidatableObject
{
    /// <summary>
    ///     Окно времени хранения идентификаторов
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Режим обработки дубликатов
    /// </summary>
    public RateLimitMode Mode { get; set; } = RateLimitMode.Hard;

    /// <summary>
    ///     Проверить корректность настроек.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
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
