using System;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bot.Storage.EFCore.Migrations;

/// <summary>
///     Начальная миграция
/// </summary>
[DbContext(typeof(StateContext))]
[Migration("20240517000000_Initial")]
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "states",
            columns: table => new
            {
                Scope = table.Column<string>(type: "text", nullable: false),
                Key = table.Column<string>(type: "text", nullable: false),
                Value = table.Column<string>(type: "text", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                TtlUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_states", x => new { x.Scope, x.Key });
            });
        migrationBuilder.CreateIndex(
            name: "IX_states_TtlUtc",
            table: "states",
            column: "TtlUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "states");
    }
}
