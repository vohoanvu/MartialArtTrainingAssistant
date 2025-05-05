using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class LinkVideoToImportedTechniquesAndDrills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drills_HumanFeedbacks_FeedbackId",
                table: "Drills");

            migrationBuilder.DropForeignKey(
                name: "FK_Fighters_Instructor_InstructorId",
                table: "Fighters");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedbacks_AiFeedbacks_AiFeedbackId",
                table: "HumanFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedbacks_Techniques_TechniqueId",
                table: "HumanFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedbacks_UploadedVideos_VideoId",
                table: "HumanFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedbacks_app_users_InstructorId",
                table: "HumanFeedbacks");

            migrationBuilder.DropTable(
                name: "AiImprovementArea");

            migrationBuilder.DropTable(
                name: "AiStrength");

            migrationBuilder.DropTable(
                name: "AiSuggestedDrill");

            migrationBuilder.DropTable(
                name: "AiTechniqueIdentification");

            migrationBuilder.DropTable(
                name: "Instructor");

            migrationBuilder.DropIndex(
                name: "IX_Fighters_InstructorId",
                table: "Fighters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HumanFeedbacks",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Fighters");

            migrationBuilder.RenameTable(
                name: "HumanFeedbacks",
                newName: "HumanFeedback");

            migrationBuilder.RenameColumn(
                name: "FeedbackText",
                table: "HumanFeedback",
                newName: "Description");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedbacks_VideoId",
                table: "HumanFeedback",
                newName: "IX_HumanFeedback_VideoId");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedbacks_TechniqueId",
                table: "HumanFeedback",
                newName: "IX_HumanFeedback_TechniqueId");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedbacks_InstructorId",
                table: "HumanFeedback",
                newName: "IX_HumanFeedback_InstructorId");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedbacks_AiFeedbackId",
                table: "HumanFeedback",
                newName: "IX_HumanFeedback_AiFeedbackId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 5, 16, 53, 38, 616, DateTimeKind.Utc).AddTicks(8880),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 2, 16, 54, 6, 315, DateTimeKind.Utc).AddTicks(410));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 5, 16, 53, 38, 616, DateTimeKind.Utc).AddTicks(8410),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 2, 16, 54, 6, 314, DateTimeKind.Utc).AddTicks(9900));

            migrationBuilder.AddColumn<int>(
                name: "AiAnalysisResultId",
                table: "Techniques",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadedVideoId",
                table: "Techniques",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiAnalysisResultId",
                table: "Drills",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadedVideoId",
                table: "Drills",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechniqueId",
                table: "AiFeedbacks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AreasForImprovement",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Strengths",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HumanFeedback",
                table: "HumanFeedback",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Techniques_AiAnalysisResultId",
                table: "Techniques",
                column: "AiAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Techniques_UploadedVideoId",
                table: "Techniques",
                column: "UploadedVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_AiAnalysisResultId",
                table: "Drills",
                column: "AiAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_UploadedVideoId",
                table: "Drills",
                column: "UploadedVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_TechniqueId",
                table: "AiFeedbacks",
                column: "TechniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_AiFeedbacks_Techniques_TechniqueId",
                table: "AiFeedbacks",
                column: "TechniqueId",
                principalTable: "Techniques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Drills_AiAnalysisResults_AiAnalysisResultId",
                table: "Drills",
                column: "AiAnalysisResultId",
                principalTable: "AiAnalysisResults",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Drills_HumanFeedback_FeedbackId",
                table: "Drills",
                column: "FeedbackId",
                principalTable: "HumanFeedback",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Drills_UploadedVideos_UploadedVideoId",
                table: "Drills",
                column: "UploadedVideoId",
                principalTable: "UploadedVideos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedback_AiFeedbacks_AiFeedbackId",
                table: "HumanFeedback",
                column: "AiFeedbackId",
                principalTable: "AiFeedbacks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedback_Techniques_TechniqueId",
                table: "HumanFeedback",
                column: "TechniqueId",
                principalTable: "Techniques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedback_UploadedVideos_VideoId",
                table: "HumanFeedback",
                column: "VideoId",
                principalTable: "UploadedVideos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedback_app_users_InstructorId",
                table: "HumanFeedback",
                column: "InstructorId",
                principalTable: "app_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Techniques_AiAnalysisResults_AiAnalysisResultId",
                table: "Techniques",
                column: "AiAnalysisResultId",
                principalTable: "AiAnalysisResults",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Techniques_UploadedVideos_UploadedVideoId",
                table: "Techniques",
                column: "UploadedVideoId",
                principalTable: "UploadedVideos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiFeedbacks_Techniques_TechniqueId",
                table: "AiFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Drills_AiAnalysisResults_AiAnalysisResultId",
                table: "Drills");

            migrationBuilder.DropForeignKey(
                name: "FK_Drills_HumanFeedback_FeedbackId",
                table: "Drills");

            migrationBuilder.DropForeignKey(
                name: "FK_Drills_UploadedVideos_UploadedVideoId",
                table: "Drills");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedback_AiFeedbacks_AiFeedbackId",
                table: "HumanFeedback");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedback_Techniques_TechniqueId",
                table: "HumanFeedback");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedback_UploadedVideos_VideoId",
                table: "HumanFeedback");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedback_app_users_InstructorId",
                table: "HumanFeedback");

            migrationBuilder.DropForeignKey(
                name: "FK_Techniques_AiAnalysisResults_AiAnalysisResultId",
                table: "Techniques");

            migrationBuilder.DropForeignKey(
                name: "FK_Techniques_UploadedVideos_UploadedVideoId",
                table: "Techniques");

            migrationBuilder.DropIndex(
                name: "IX_Techniques_AiAnalysisResultId",
                table: "Techniques");

            migrationBuilder.DropIndex(
                name: "IX_Techniques_UploadedVideoId",
                table: "Techniques");

            migrationBuilder.DropIndex(
                name: "IX_Drills_AiAnalysisResultId",
                table: "Drills");

            migrationBuilder.DropIndex(
                name: "IX_Drills_UploadedVideoId",
                table: "Drills");

            migrationBuilder.DropIndex(
                name: "IX_AiFeedbacks_TechniqueId",
                table: "AiFeedbacks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HumanFeedback",
                table: "HumanFeedback");

            migrationBuilder.DropColumn(
                name: "AiAnalysisResultId",
                table: "Techniques");

            migrationBuilder.DropColumn(
                name: "UploadedVideoId",
                table: "Techniques");

            migrationBuilder.DropColumn(
                name: "AiAnalysisResultId",
                table: "Drills");

            migrationBuilder.DropColumn(
                name: "UploadedVideoId",
                table: "Drills");

            migrationBuilder.DropColumn(
                name: "TechniqueId",
                table: "AiFeedbacks");

            migrationBuilder.DropColumn(
                name: "AreasForImprovement",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "Strengths",
                table: "AiAnalysisResults");

            migrationBuilder.RenameTable(
                name: "HumanFeedback",
                newName: "HumanFeedbacks");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "HumanFeedbacks",
                newName: "FeedbackText");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedback_VideoId",
                table: "HumanFeedbacks",
                newName: "IX_HumanFeedbacks_VideoId");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedback_TechniqueId",
                table: "HumanFeedbacks",
                newName: "IX_HumanFeedbacks_TechniqueId");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedback_InstructorId",
                table: "HumanFeedbacks",
                newName: "IX_HumanFeedbacks_InstructorId");

            migrationBuilder.RenameIndex(
                name: "IX_HumanFeedback_AiFeedbackId",
                table: "HumanFeedbacks",
                newName: "IX_HumanFeedbacks_AiFeedbackId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 2, 16, 54, 6, 315, DateTimeKind.Utc).AddTicks(410),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 5, 16, 53, 38, 616, DateTimeKind.Utc).AddTicks(8880));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 2, 16, 54, 6, 314, DateTimeKind.Utc).AddTicks(9900),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 5, 16, 53, 38, 616, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Fighters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HumanFeedbacks",
                table: "HumanFeedbacks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AiImprovementArea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AiAnalysisResultId = table.Column<int>(type: "integer", nullable: false),
                    FighterIdentifier = table.Column<string>(type: "text", nullable: true),
                    ImprovementDescription = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiImprovementArea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiImprovementArea_AiAnalysisResults_AiAnalysisResultId",
                        column: x => x.AiAnalysisResultId,
                        principalTable: "AiAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiStrength",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AiAnalysisResultId = table.Column<int>(type: "integer", nullable: false),
                    FighterIdentifier = table.Column<string>(type: "text", nullable: true),
                    StrengthDescription = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiStrength", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiStrength_AiAnalysisResults_AiAnalysisResultId",
                        column: x => x.AiAnalysisResultId,
                        principalTable: "AiAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiSuggestedDrill",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AiAnalysisResultId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<string>(type: "text", nullable: true),
                    Focus = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSuggestedDrill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiSuggestedDrill_AiAnalysisResults_AiAnalysisResultId",
                        column: x => x.AiAnalysisResultId,
                        principalTable: "AiAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiTechniqueIdentification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AiAnalysisResultId = table.Column<int>(type: "integer", nullable: false),
                    TechniqueId = table.Column<int>(type: "integer", nullable: false),
                    FighterIdentifier = table.Column<string>(type: "text", nullable: true),
                    Timespan = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTechniqueIdentification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiTechniqueIdentification_AiAnalysisResults_AiAnalysisResul~",
                        column: x => x.AiAnalysisResultId,
                        principalTable: "AiAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiTechniqueIdentification_Techniques_TechniqueId",
                        column: x => x.TechniqueId,
                        principalTable: "Techniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Instructor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Specialization = table.Column<string>(type: "text", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instructor", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fighters_InstructorId",
                table: "Fighters",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_AiImprovementArea_AiAnalysisResultId",
                table: "AiImprovementArea",
                column: "AiAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AiStrength_AiAnalysisResultId",
                table: "AiStrength",
                column: "AiAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestedDrill_AiAnalysisResultId",
                table: "AiSuggestedDrill",
                column: "AiAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTechniqueIdentification_AiAnalysisResultId",
                table: "AiTechniqueIdentification",
                column: "AiAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTechniqueIdentification_TechniqueId",
                table: "AiTechniqueIdentification",
                column: "TechniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drills_HumanFeedbacks_FeedbackId",
                table: "Drills",
                column: "FeedbackId",
                principalTable: "HumanFeedbacks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Fighters_Instructor_InstructorId",
                table: "Fighters",
                column: "InstructorId",
                principalTable: "Instructor",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedbacks_AiFeedbacks_AiFeedbackId",
                table: "HumanFeedbacks",
                column: "AiFeedbackId",
                principalTable: "AiFeedbacks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedbacks_Techniques_TechniqueId",
                table: "HumanFeedbacks",
                column: "TechniqueId",
                principalTable: "Techniques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedbacks_UploadedVideos_VideoId",
                table: "HumanFeedbacks",
                column: "VideoId",
                principalTable: "UploadedVideos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HumanFeedbacks_app_users_InstructorId",
                table: "HumanFeedbacks",
                column: "InstructorId",
                principalTable: "app_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
