using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Semogly.SummonersHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualCraftItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WizardId = table.Column<long>(type: "bigint", nullable: false),
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualCraftItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManualMonsters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WizardId = table.Column<long>(type: "bigint", nullable: false),
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    MasterId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualMonsters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualCraftItems_WizardId_GameId",
                table: "ManualCraftItems",
                columns: new[] { "WizardId", "GameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualMonsters_WizardId_GameId",
                table: "ManualMonsters",
                columns: new[] { "WizardId", "GameId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualCraftItems");

            migrationBuilder.DropTable(
                name: "ManualMonsters");
        }
    }
}
