using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stalinon.Bot.Admin.MinimalApi;

/// <summary>
///     Настройки административного API.
/// </summary>
public sealed class AdminOptions : IValidatableObject
{
    /// <summary>
    ///     Токен администратора.
    /// </summary>
    public string AdminToken { get; set; } = string.Empty;

    /// <summary>
    ///     Проверить корректность настроек.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(AdminToken))
        {
            yield return new ValidationResult("Токен администратора обязателен", [nameof(AdminToken)]);
        }
    }
}
