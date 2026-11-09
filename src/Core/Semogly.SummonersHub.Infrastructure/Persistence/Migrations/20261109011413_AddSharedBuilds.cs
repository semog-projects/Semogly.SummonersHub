using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Semogly.SummonersHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedBuilds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedBuilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character(22)", fixedLength: true, maxLength: 22, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WizardId = table.Column<long>(type: "bigint", nullable: false),
                    MonsterId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedBuilds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedBuilds_AccountSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "AccountSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedBuilds_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedBuilds_OwnerId_WizardId_MonsterId",
                table: "SharedBuilds",
                columns: new[] { "OwnerId", "WizardId", "MonsterId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedBuilds_SnapshotId",
                table: "SharedBuilds",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedBuilds_Token",
                table: "SharedBuilds",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedBuilds");
        }
    }
}
