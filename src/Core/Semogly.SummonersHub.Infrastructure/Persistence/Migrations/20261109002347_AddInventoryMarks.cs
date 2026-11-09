using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Semogly.SummonersHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryMarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_WizardAccounts_WizardId",
                table: "WizardAccounts",
                column: "WizardId");

            migrationBuilder.CreateTable(
                name: "InventoryMarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WizardId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMarks_WizardAccounts_WizardId",
                        column: x => x.WizardId,
                        principalTable: "WizardAccounts",
                        principalColumn: "WizardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMarks_WizardId_Kind_ItemId",
                table: "InventoryMarks",
                columns: new[] { "WizardId", "Kind", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryMarks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WizardAccounts_WizardId",
                table: "WizardAccounts");
        }
    }
}
