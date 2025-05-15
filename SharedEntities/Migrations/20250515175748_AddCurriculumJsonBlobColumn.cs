using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumJsonBlobColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(8150),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 12, 20, 10, 53, 91, DateTimeKind.Utc).AddTicks(5220));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(7680),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 12, 20, 10, 53, 91, DateTimeKind.Utc).AddTicks(4780));

            migrationBuilder.AddColumn<string>(
                name: "EditedCurriculumJson",
                table: "TrainingSessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawCurriculumJson",
                table: "TrainingSessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_EditedCurriculumJson",
                table: "TrainingSessions",
                column: "EditedCurriculumJson")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_RawCurriculumJson",
                table: "TrainingSessions",
                column: "RawCurriculumJson")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingSessions_EditedCurriculumJson",
                table: "TrainingSessions");

            migrationBuilder.DropIndex(
                name: "IX_TrainingSessions_RawCurriculumJson",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "EditedCurriculumJson",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "RawCurriculumJson",
                table: "TrainingSessions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 12, 20, 10, 53, 91, DateTimeKind.Utc).AddTicks(5220),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(8150));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 12, 20, 10, 53, 91, DateTimeKind.Utc).AddTicks(4780),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(7680));
        }
    }
}
