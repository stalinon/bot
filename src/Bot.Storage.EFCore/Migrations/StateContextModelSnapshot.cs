using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bot.Storage.EFCore.Migrations;

/// <summary>
///     Снимок модели для миграций
/// </summary>
[DbContext(typeof(StateContext))]
public partial class StateContextModelSnapshot : ModelSnapshot
{
    /// <inheritdoc />
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

        modelBuilder.Entity<StateEntry>(b =>
        {
            b.Property<string>("Scope").HasColumnType("text");
            b.Property<string>("Key").HasColumnType("text");
            b.Property<string>("Value").HasColumnType("text");
            b.Property<DateTimeOffset>("UpdatedUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("TtlUtc").HasColumnType("timestamp with time zone");
            b.Property<long>("Version").HasColumnType("bigint");
            b.HasKey("Scope", "Key");
            b.HasIndex("TtlUtc");
            b.ToTable("states");
        });
    }
}
