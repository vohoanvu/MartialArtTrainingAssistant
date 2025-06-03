using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddFighterPairsJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(8020),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 27, 11, 25, 36, 86, DateTimeKind.Utc).AddTicks(4510));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(7140),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 27, 11, 25, 36, 86, DateTimeKind.Utc).AddTicks(3830));

            migrationBuilder.AddColumn<string>(
                name: "EditedFighterPairsJson",
                table: "TrainingSessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawFighterPairsJson",
                table: "TrainingSessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_RawFighterPairsJson",
                table: "TrainingSessions",
                column: "RawFighterPairsJson")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingSessions_RawFighterPairsJson",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "EditedFighterPairsJson",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "RawFighterPairsJson",
                table: "TrainingSessions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 27, 11, 25, 36, 86, DateTimeKind.Utc).AddTicks(4510),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 27, 11, 25, 36, 86, DateTimeKind.Utc).AddTicks(3830),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(7140));
        }
    }
}
