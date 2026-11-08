using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Semogly.SummonersHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WizardId = table.Column<long>(type: "bigint", nullable: false),
                    WizardName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WizardLevel = table.Column<int>(type: "integer", nullable: false),
                    WizardServerId = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Artifacts",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Element = table.Column<int>(type: "integer", nullable: true),
                    Archetype = table.Column<int>(type: "integer", nullable: true),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    OriginalRarity = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    MainStat_Effect = table.Column<int>(type: "integer", nullable: false),
                    MainStat_Value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EquippedMonsterId = table.Column<long>(type: "bigint", nullable: true),
                    Substats = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => new { x.SnapshotId, x.GameId });
                    table.ForeignKey(
                        name: "FK_Artifacts_AccountSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "AccountSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CraftItems",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftItems", x => new { x.SnapshotId, x.GameId });
                    table.ForeignKey(
                        name: "FK_CraftItems_AccountSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "AccountSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Monsters",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    Element = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_Hp = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_Attack = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_Defense = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_Speed = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_Resistance = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_Accuracy = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_CriticalRate = table.Column<int>(type: "integer", nullable: false),
                    BaseStats_CriticalDamage = table.Column<int>(type: "integer", nullable: false),
                    IsHomunculus = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Skills = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monsters", x => new { x.SnapshotId, x.GameId });
                    table.ForeignKey(
                        name: "FK_Monsters_AccountSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "AccountSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Runes",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    IsAncient = table.Column<bool>(type: "boolean", nullable: false),
                    Set = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    OriginalRarity = table.Column<int>(type: "integer", nullable: false),
                    MainStat_Stat = table.Column<int>(type: "integer", nullable: false),
                    MainStat_Value = table.Column<int>(type: "integer", nullable: false),
                    InnateStat_Stat = table.Column<int>(type: "integer", nullable: true),
                    InnateStat_Value = table.Column<int>(type: "integer", nullable: true),
                    EquippedMonsterId = table.Column<long>(type: "bigint", nullable: true),
                    Substats = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runes", x => new { x.SnapshotId, x.GameId });
                    table.ForeignKey(
                        name: "FK_Runes_AccountSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "AccountSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSnapshots_WizardId_ContentHash",
                table: "AccountSnapshots",
                columns: new[] { "WizardId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSnapshots_WizardId_Version",
                table: "AccountSnapshots",
                columns: new[] { "WizardId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_SnapshotId_EquippedMonsterId",
                table: "Artifacts",
                columns: new[] { "SnapshotId", "EquippedMonsterId" });

            migrationBuilder.CreateIndex(
                name: "IX_Runes_SnapshotId_EquippedMonsterId",
                table: "Runes",
                columns: new[] { "SnapshotId", "EquippedMonsterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Artifacts");

            migrationBuilder.DropTable(
                name: "CraftItems");

            migrationBuilder.DropTable(
                name: "Monsters");

            migrationBuilder.DropTable(
                name: "Runes");

            migrationBuilder.DropTable(
                name: "AccountSnapshots");
        }
    }
}
