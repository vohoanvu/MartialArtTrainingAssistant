using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoTranscodeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaybackFilePath",
                table: "Videos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TranscodeStatus",
                table: "Videos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaybackFilePath",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "TranscodeStatus",
                table: "Videos");
        }
    }
}
