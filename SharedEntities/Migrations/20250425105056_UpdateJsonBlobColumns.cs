using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJsonBlobColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(6430),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3608));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(5960),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3364));

            migrationBuilder.AlterColumn<double>(
                name: "Timestamp",
                table: "HumanFeedbacks",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.Sql("ALTER TABLE \"HumanFeedbacks\" ALTER COLUMN \"AIAnalysisJson\" TYPE jsonb USING \"AIAnalysisJson\"::jsonb;");

            migrationBuilder.AddColumn<string>(
                name: "FeedbackType",
                table: "HumanFeedbacks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("ALTER TABLE \"AiFeedbacks\" ALTER COLUMN \"AnalysisJson\" TYPE jsonb USING \"AnalysisJson\"::jsonb;");

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

            migrationBuilder.Sql("ALTER TABLE \"AiAnalysisResults\" ALTER COLUMN \"AnalysisJson\" TYPE jsonb USING \"AnalysisJson\"::jsonb;");

            migrationBuilder.CreateIndex(
                name: "IX_HumanFeedbacks_AIAnalysisJson",
                table: "HumanFeedbacks",
                column: "AIAnalysisJson")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_AnalysisJson",
                table: "AiFeedbacks",
                column: "AnalysisJson")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisResults_AnalysisJson",
                table: "AiAnalysisResults",
                column: "AnalysisJson")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HumanFeedbacks_AIAnalysisJson",
                table: "HumanFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_AiFeedbacks_AnalysisJson",
                table: "AiFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_AiAnalysisResults_AnalysisJson",
                table: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "FeedbackType",
                table: "HumanFeedbacks");

            migrationBuilder.DropColumn(
                name: "FeedbackType",
                table: "AiFeedbacks");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "AiFeedbacks");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3608),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(6430));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 3, 31, 13, 1, 22, 77, DateTimeKind.Utc).AddTicks(3364),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 4, 25, 10, 50, 55, 867, DateTimeKind.Utc).AddTicks(5960));

            migrationBuilder.AlterColumn<double>(
                name: "Timestamp",
                table: "HumanFeedbacks",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.Sql("ALTER TABLE \"HumanFeedbacks\" ALTER COLUMN \"AIAnalysisJson\" TYPE text USING \"AIAnalysisJson\"::text;");

            migrationBuilder.Sql("ALTER TABLE \"AiFeedbacks\" ALTER COLUMN \"AnalysisJson\" TYPE text USING \"AnalysisJson\"::text;");

            migrationBuilder.Sql("ALTER TABLE \"AiAnalysisResults\" ALTER COLUMN \"AnalysisJson\" TYPE text USING \"AnalysisJson\"::text;");
        }
    }
}