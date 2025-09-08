namespace Stalinon.Bot.Scheduler;

/// <summary>
///     Описание фоновой задачи.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Содержит тип и расписание</item>
///         <item>Поддерживает cron и интервалы</item>
///     </list>
/// </remarks>
public sealed record JobDescriptor(Type JobType, string? Cron, TimeSpan? Interval);
