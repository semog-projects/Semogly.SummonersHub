using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Semogly.SummonersHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlternativeBuilds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlternativeBuilds",
                columns: table => new
                {
                    MonsterId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DeckId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuneIds = table.Column<List<long>>(type: "bigint[]", nullable: false),
                    ArtifactIds = table.Column<List<long>>(type: "bigint[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlternativeBuilds", x => new { x.SnapshotId, x.MonsterId, x.Kind, x.DeckId });
                    table.ForeignKey(
                        name: "FK_AlternativeBuilds_AccountSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "AccountSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlternativeBuilds");
        }
    }
}
