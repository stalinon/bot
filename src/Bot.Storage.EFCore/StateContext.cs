using Microsoft.EntityFrameworkCore;

namespace Bot.Storage.EFCore;

/// <summary>
///     Контекст базы данных для состояний
/// </summary>
public sealed class StateContext : DbContext
{
    /// <summary>
    ///     Создаёт контекст
    /// </summary>
    /// <param name="options">Опции EF Core</param>
    public StateContext(DbContextOptions<StateContext> options) : base(options)
    {
    }

    /// <summary>
    ///     Таблица записей
    /// </summary>
    public DbSet<StateEntry> States => Set<StateEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StateEntry>(b =>
        {
            b.ToTable("states");
            b.HasKey(e => new { e.Scope, e.Key });
            b.HasIndex(e => e.ExpiresAt);
        });
    }
}
