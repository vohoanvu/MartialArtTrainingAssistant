using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddAgenticPipelineEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(7140));

            migrationBuilder.AddColumn<string>(
                name: "ContextCacheName",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EliteTip",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeLabel",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchSummary",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipelineError",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PipelineStatus",
                table: "AiAnalysisResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TechnicalGrade",
                table: "AiAnalysisResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisualDna",
                table: "AiAnalysisResults",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachingReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AiAnalysisResultId = table.Column<int>(type: "integer", nullable: false),
                    MatchSummary = table.Column<string>(type: "text", nullable: false),
                    TechnicalGrade = table.Column<int>(type: "integer", nullable: false),
                    GradeLabel = table.Column<string>(type: "text", nullable: true),
                    EliteTip = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachingReports_AiAnalysisResults_AiAnalysisResultId",
                        column: x => x.AiAnalysisResultId,
                        principalTable: "AiAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AiAnalysisResultId = table.Column<int>(type: "integer", nullable: false),
                    StartTimestampMs = table.Column<int>(type: "integer", nullable: false),
                    EndTimestampMs = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<int>(type: "integer", nullable: false),
                    TechniqueCategory = table.Column<string>(type: "text", nullable: false),
                    TechniqueName = table.Column<string>(type: "text", nullable: true),
                    PositionBefore = table.Column<string>(type: "text", nullable: true),
                    PositionAfter = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    GuardType = table.Column<string>(type: "text", nullable: true),
                    SubmissionType = table.Column<string>(type: "text", nullable: true),
                    ActionsDescription = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    SequenceIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchEvents_AiAnalysisResults_AiAnalysisResultId",
                        column: x => x.AiAnalysisResultId,
                        principalTable: "AiAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachingStrengths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CoachingReportId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    TimestampStartMs = table.Column<int>(type: "integer", nullable: true),
                    TimestampEndMs = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingStrengths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachingStrengths_CoachingReports_CoachingReportId",
                        column: x => x.CoachingReportId,
                        principalTable: "CoachingReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachingWeaknesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CoachingReportId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    TimestampStartMs = table.Column<int>(type: "integer", nullable: true),
                    TimestampEndMs = table.Column<int>(type: "integer", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    ScoringImpact = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingWeaknesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachingWeaknesses_CoachingReports_CoachingReportId",
                        column: x => x.CoachingReportId,
                        principalTable: "CoachingReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescribedDrills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CoachingReportId = table.Column<int>(type: "integer", nullable: false),
                    DrillName = table.Column<string>(type: "text", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: true),
                    Goal = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescribedDrills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescribedDrills_CoachingReports_CoachingReportId",
                        column: x => x.CoachingReportId,
                        principalTable: "CoachingReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachingReports_AiAnalysisResultId",
                table: "CoachingReports",
                column: "AiAnalysisResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachingStrengths_CoachingReportId",
                table: "CoachingStrengths",
                column: "CoachingReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachingWeaknesses_CoachingReportId",
                table: "CoachingWeaknesses",
                column: "CoachingReportId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_AiAnalysisResultId_SequenceIndex",
                table: "MatchEvents",
                columns: new[] { "AiAnalysisResultId", "SequenceIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescribedDrills_CoachingReportId",
                table: "PrescribedDrills",
                column: "CoachingReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachingStrengths");

            migrationBuilder.DropTable(
                name: "CoachingWeaknesses");

            migrationBuilder.DropTable(
                name: "MatchEvents");

            migrationBuilder.DropTable(
                name: "PrescribedDrills");

            migrationBuilder.DropTable(
                name: "CoachingReports");

            migrationBuilder.DropColumn(
                name: "ContextCacheName",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "EliteTip",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "GradeLabel",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "MatchSummary",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "PipelineError",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "PipelineStatus",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "TechnicalGrade",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "VisualDna",
                table: "AiAnalysisResults");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(8020),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 6, 3, 13, 14, 28, 105, DateTimeKind.Utc).AddTicks(7140),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now() at time zone 'utc'");
        }
    }
}
