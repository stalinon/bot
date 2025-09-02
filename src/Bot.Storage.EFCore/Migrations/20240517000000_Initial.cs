using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_states", x => new { x.Scope, x.Key });
            });
        migrationBuilder.CreateIndex(
            name: "IX_states_ExpiresAt",
            table: "states",
            column: "ExpiresAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "states");
    }
}
