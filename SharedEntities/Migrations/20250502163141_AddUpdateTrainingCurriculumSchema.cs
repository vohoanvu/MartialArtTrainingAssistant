using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateTrainingCurriculumSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HumanFeedbacks_AIAnalysisJson",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "Cooldown",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "Warmup",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "AIAnalysisJson",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "FeedbackType",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "FeedbackType",
                table: "AiFeedbacks");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "AiFeedbacks");

            migrationBuilder.RenameColumn(
                name: "SessionType",
                table: "TrainingSessions",
                newName: "MartialArt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 2, 16, 31, 41, 318, DateTimeKind.Utc).AddTicks(30),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(6430));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 2, 16, 31, 41, 317, DateTimeKind.Utc).AddTicks(9520),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(5960));

            migrationBuilder.AddColumn<int>(
                name: "MartialArt",
                table: "UploadedVideos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StudentIdentifier",
                table: "UploadedVideos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FeedbackText",
                table: "HumanFeedbacks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AiFeedbackId",
                table: "HumanFeedbacks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTimestamp",
                table: "HumanFeedbacks",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTimestamp",
                table: "HumanFeedbacks",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechniqueId",
                table: "HumanFeedbacks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "TechniqueTag",
                table: "Demonstrations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AnalysisJson",
                table: "AiFeedbacks",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTimestamp",
                table: "AiFeedbacks",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTimestamp",
                table: "AiFeedbacks",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverallDescription",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

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
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Focus = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<string>(type: "text", nullable: true)
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
                name: "Curriculums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Module = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointScoringTechniques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    MartialArt = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointScoringTechniques", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PositionalScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FocusModule = table.Column<int>(type: "integer", nullable: false),
                    TargetLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionalScenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurriculumId = table.Column<int>(type: "integer", nullable: false),
                    PositionalScenarioId = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumScenarios_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurriculumScenarios_PositionalScenarios_PositionalScenarioId",
                        column: x => x.PositionalScenarioId,
                        principalTable: "PositionalScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechniqueTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PositionalScenarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechniqueTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechniqueTypes_PositionalScenarios_PositionalScenarioId",
                        column: x => x.PositionalScenarioId,
                        principalTable: "PositionalScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Techniques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    TechniqueTypeId = table.Column<int>(type: "integer", nullable: false),
                    DemonstrationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Techniques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Techniques_Demonstrations_DemonstrationId",
                        column: x => x.DemonstrationId,
                        principalTable: "Demonstrations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Techniques_PointScoringTechniques_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "PointScoringTechniques",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Techniques_TechniqueTypes_TechniqueTypeId",
                        column: x => x.TechniqueTypeId,
                        principalTable: "TechniqueTypes",
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
                    Timespan = table.Column<TimeSpan>(type: "interval", nullable: true),
                    FighterIdentifier = table.Column<string>(type: "text", nullable: true)
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
                name: "Drills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Focus = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    TechniqueId = table.Column<int>(type: "integer", nullable: false),
                    FeedbackId = table.Column<int>(type: "integer", nullable: true),
                    DemonstrationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drills_Demonstrations_DemonstrationId",
                        column: x => x.DemonstrationId,
                        principalTable: "Demonstrations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Drills_HumanFeedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "HumanFeedbacks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Drills_Techniques_TechniqueId",
                        column: x => x.TechniqueId,
                        principalTable: "Techniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessionTechniqueJoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    TechniqueId = table.Column<int>(type: "integer", nullable: false),
                    DrillId = table.Column<int>(type: "integer", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessionTechniqueJoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessionTechniqueJoints_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSessionTechniqueJoints_Techniques_TechniqueId",
                        column: x => x.TechniqueId,
                        principalTable: "Techniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingSessionTechniqueJoints_TrainingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedbacks_AiFeedbackId",
                table: "HumanFeedbacks",
                column: "AiFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedbacks_TechniqueId",
                table: "HumanFeedbacks",
                column: "TechniqueId");

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

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumScenarios_CurriculumId",
                table: "CurriculumScenarios",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumScenarios_PositionalScenarioId",
                table: "CurriculumScenarios",
                column: "PositionalScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_DemonstrationId",
                table: "Drills",
                column: "DemonstrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_FeedbackId",
                table: "Drills",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_TechniqueId",
                table: "Drills",
                column: "TechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_TechniqueTypes_PositionalScenarioId",
                table: "TechniqueTypes",
                column: "PositionalScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Techniques_CategoryId",
                table: "Techniques",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Techniques_DemonstrationId",
                table: "Techniques",
                column: "DemonstrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Techniques_TechniqueTypeId",
                table: "Techniques",
                column: "TechniqueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessionTechniqueJoints_DrillId",
                table: "TrainingSessionTechniqueJoints",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessionTechniqueJoints_SessionId",
                table: "TrainingSessionTechniqueJoints",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessionTechniqueJoints_TechniqueId",
                table: "TrainingSessionTechniqueJoints",
                column: "TechniqueId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedbacks_AiFeedbacks_AiFeedbackId",
                table: "HumanFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanFeedbacks_Techniques_TechniqueId",
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
                name: "CurriculumScenarios");

            migrationBuilder.DropTable(
                name: "TrainingSessionTechniqueJoints");

            migrationBuilder.DropTable(
                name: "Curriculums");

            migrationBuilder.DropTable(
                name: "Drills");

            migrationBuilder.DropTable(
                name: "Techniques");

            migrationBuilder.DropTable(
                name: "PointScoringTechniques");

            migrationBuilder.DropTable(
                name: "TechniqueTypes");

            migrationBuilder.DropTable(
                name: "PositionalScenarios");

            migrationBuilder.DropIndex(
                name: "IX_HumanFeedbacks_AiFeedbackId",
                table: "HumanFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_HumanFeedbacks_TechniqueId",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "MartialArt",
                table: "UploadedVideos");

            migrationBuilder.DropColumn(
                name: "StudentIdentifier",
                table: "UploadedVideos");

            migrationBuilder.DropColumn(
                name: "AiFeedbackId",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "EndTimestamp",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "StartTimestamp",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "TechniqueId",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "EndTimestamp",
                table: "AiFeedbacks");

            migrationBuilder.DropColumn(
                name: "StartTimestamp",
                table: "AiFeedbacks");

            migrationBuilder.DropColumn(
                name: "OverallDescription",
                table: "AiAnalysisResults");

            migrationBuilder.RenameColumn(
                name: "MartialArt",
                table: "TrainingSessions",
                newName: "SessionType");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(6430),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 2, 16, 31, 41, 318, DateTimeKind.Utc).AddTicks(30));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(5960),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 2, 16, 31, 41, 317, DateTimeKind.Utc).AddTicks(9520));

            migrationBuilder.AddColumn<string>(
                name: "Cooldown",
                table: "TrainingSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Warmup",
                table: "TrainingSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FeedbackText",
                table: "HumanFeedbacks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIAnalysisJson",
                table: "HumanFeedbacks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackType",
                table: "HumanFeedbacks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Timestamp",
                table: "HumanFeedbacks",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "TechniqueTag",
                table: "Demonstrations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AnalysisJson",
                table: "AiFeedbacks",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackType",
                table: "AiFeedbacks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Timestamp",
                table: "AiFeedbacks",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedbacks_AIAnalysisJson",
                table: "HumanFeedbacks",
                column: "AIAnalysisJson")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }
    }
}
