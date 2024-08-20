using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FighterManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class CreateFighterTrainingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5595),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4777));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5376),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4544));

            migrationBuilder.AddColumn<int>(
                name: "FighterId",
                table: "app_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Fighters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FighterName = table.Column<string>(type: "text", nullable: false),
                    Height = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    BMI = table.Column<double>(type: "double precision", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Birthdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxWorkoutDuration = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    BelkRank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fighters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TrainingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InstructorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_Fighters_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Fighters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessionFighterJoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    FighterId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessionFighterJoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessionFighterJoints_Fighters_FighterId",
                        column: x => x.FighterId,
                        principalTable: "Fighters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingSessionFighterJoints_TrainingSessions_TrainingSessi~",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_users_FighterId",
                table: "app_users",
                column: "FighterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessionFighterJoints_FighterId",
                table: "TrainingSessionFighterJoints",
                column: "FighterId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessionFighterJoints_TrainingSessionId",
                table: "TrainingSessionFighterJoints",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_InstructorId",
                table: "TrainingSessions",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_app_users_Fighters_FighterId",
                table: "app_users",
                column: "FighterId",
                principalTable: "Fighters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_app_users_Fighters_FighterId",
                table: "app_users");

            migrationBuilder.DropTable(
                name: "TrainingSessionFighterJoints");

            migrationBuilder.DropTable(
                name: "TrainingSessions");

            migrationBuilder.DropTable(
                name: "Fighters");

            migrationBuilder.DropIndex(
                name: "IX_app_users_FighterId",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "FighterId",
                table: "app_users");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4777),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5595));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4544),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 7, 30, 15, 36, 56, 353, DateTimeKind.Utc).AddTicks(5376));
        }
    }
}
