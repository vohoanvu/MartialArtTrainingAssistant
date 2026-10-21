using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFeatureConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiFeatureConfigs",
                columns: table => new
                {
                    FeatureKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFeatureConfigs", x => x.FeatureKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiFeatureConfigs");
        }
    }
}
