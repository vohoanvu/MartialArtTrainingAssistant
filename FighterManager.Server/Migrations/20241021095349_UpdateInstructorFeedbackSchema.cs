using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FighterManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInstructorFeedbackSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "TrainingSessions",
                newName: "Warmup");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 227, DateTimeKind.Utc).AddTicks(113),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5595));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 226, DateTimeKind.Utc).AddTicks(9871),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5376));

            migrationBuilder.AddColumn<string>(
                name: "Cooldown",
                table: "TrainingSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionNotes",
                table: "TrainingSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionType",
                table: "TrainingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetLevel",
                table: "TrainingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Fighters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Instructor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false),
                    Specialization = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instructor", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fighters_InstructorId",
                table: "Fighters",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fighters_Instructor_InstructorId",
                table: "Fighters",
                column: "InstructorId",
                principalTable: "Instructor",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fighters_Instructor_InstructorId",
                table: "Fighters");

            migrationBuilder.DropTable(
                name: "Instructor");

            migrationBuilder.DropIndex(
                name: "IX_Fighters_InstructorId",
                table: "Fighters");

            migrationBuilder.DropColumn(
                name: "Cooldown",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "SessionNotes",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "TargetLevel",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Fighters");

            migrationBuilder.RenameColumn(
                name: "Warmup",
                table: "TrainingSessions",
                newName: "Description");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5595),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 227, DateTimeKind.Utc).AddTicks(113));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5376),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 226, DateTimeKind.Utc).AddTicks(9871));
        }
    }
}
