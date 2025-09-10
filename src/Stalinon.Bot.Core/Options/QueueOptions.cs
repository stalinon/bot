using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stalinon.Bot.Core.Options;

/// <summary>
///     Настройки очереди обновлений.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяет политику обработки переполнения.</item>
///     </list>
/// </remarks>
public sealed class QueueOptions : IValidatableObject
{
    /// <summary>
    ///     Политика заполнения очереди.
    /// </summary>
    public QueuePolicy Policy { get; set; } = QueuePolicy.Wait;

    /// <summary>
    ///     Проверить корректность настроек.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Enum.IsDefined(typeof(QueuePolicy), Policy))
        {
            yield return new ValidationResult("Некорректная политика", [nameof(Policy)]);
        }
    }
}
