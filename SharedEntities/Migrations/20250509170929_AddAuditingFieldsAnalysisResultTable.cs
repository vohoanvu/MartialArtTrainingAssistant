using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditingFieldsAnalysisResultTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drills_HumanFeedback_FeedbackId",
                table: "Drills");

            migrationBuilder.DropTable(
                name: "HumanFeedback");

            migrationBuilder.DropIndex(
                name: "IX_Drills_FeedbackId",
                table: "Drills");

            migrationBuilder.DropColumn(
                name: "FeedbackId",
                table: "Drills");

            migrationBuilder.RenameColumn(
                name: "UploadTimestamp",
                table: "UploadedVideos",
                newName: "UploadedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 9, 17, 9, 29, 169, DateTimeKind.Utc).AddTicks(3840),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(930));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 9, 17, 9, 29, 169, DateTimeKind.Utc).AddTicks(3400),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(340));

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "UploadedVideos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "AiAnalysisResults",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "AiAnalysisResults",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "UploadedVideos");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "AiAnalysisResults");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "UploadedVideos",
                newName: "UploadTimestamp");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(930),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 9, 17, 9, 29, 169, DateTimeKind.Utc).AddTicks(3840));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(340),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 9, 17, 9, 29, 169, DateTimeKind.Utc).AddTicks(3400));

            migrationBuilder.AddColumn<int>(
                name: "FeedbackId",
                table: "Drills",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HumanFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AiFeedbackId = table.Column<int>(type: "integer", nullable: true),
                    InstructorId = table.Column<string>(type: "text", nullable: false),
                    TechniqueId = table.Column<int>(type: "integer", nullable: false),
                    VideoId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EndTimestamp = table.Column<TimeSpan>(type: "interval", nullable: true),
                    StartTimestamp = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanFeedback_AiFeedbacks_AiFeedbackId",
                        column: x => x.AiFeedbackId,
                        principalTable: "AiFeedbacks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HumanFeedback_Techniques_TechniqueId",
                        column: x => x.TechniqueId,
                        principalTable: "Techniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HumanFeedback_UploadedVideos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "UploadedVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HumanFeedback_app_users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drills_FeedbackId",
                table: "Drills",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedback_AiFeedbackId",
                table: "HumanFeedback",
                column: "AiFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedback_InstructorId",
                table: "HumanFeedback",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedback_TechniqueId",
                table: "HumanFeedback",
                column: "TechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedback_VideoId",
                table: "HumanFeedback",
                column: "VideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drills_HumanFeedback_FeedbackId",
                table: "Drills",
                column: "FeedbackId",
                principalTable: "HumanFeedback",
                principalColumn: "Id");
        }
    }
}
