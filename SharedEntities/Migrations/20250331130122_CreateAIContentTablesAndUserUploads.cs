using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class CreateAIContentTablesAndUserUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3608),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 227, DateTimeKind.Utc).AddTicks(113));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3364),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 226, DateTimeKind.Utc).AddTicks(9871));

            migrationBuilder.CreateTable(
                name: "Demonstrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstructorId = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TechniqueTag = table.Column<string>(type: "text", nullable: false),
                    UploadTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demonstrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Demonstrations_app_users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    UploadTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AISummary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedVideos_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VideoId = table.Column<int>(type: "integer", nullable: false),
                    AnalysisJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAnalysisResults_UploadedVideos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "UploadedVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VideoId = table.Column<int>(type: "integer", nullable: false),
                    AnalysisJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiFeedbacks_UploadedVideos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "UploadedVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HumanFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VideoId = table.Column<int>(type: "integer", nullable: false),
                    InstructorId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<double>(type: "double precision", nullable: true),
                    FeedbackText = table.Column<string>(type: "text", nullable: false),
                    AIAnalysisJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanFeedbacks_UploadedVideos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "UploadedVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HumanFeedbacks_app_users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisResults_VideoId",
                table: "AiAnalysisResults",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_VideoId",
                table: "AiFeedbacks",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Demonstrations_InstructorId",
                table: "Demonstrations",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedbacks_InstructorId",
                table: "HumanFeedbacks",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedbacks_VideoId",
                table: "HumanFeedbacks",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedVideos_UserId",
                table: "UploadedVideos",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAnalysisResults");

            migrationBuilder.DropTable(
                name: "AiFeedbacks");

            migrationBuilder.DropTable(
                name: "Demonstrations");

            migrationBuilder.DropTable(
                name: "HumanFeedbacks");

            migrationBuilder.DropTable(
                name: "UploadedVideos");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 227, DateTimeKind.Utc).AddTicks(113),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3608));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 10, 21, 9, 53, 48, 226, DateTimeKind.Utc).AddTicks(9871),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3364));
        }
    }
}
