using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleAspNetReactDockerApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSharedVideoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4777),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 4, 10, 15, 35, 25, 266, DateTimeKind.Utc).AddTicks(7205));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4544),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 4, 10, 15, 35, 25, 266, DateTimeKind.Utc).AddTicks(6984));

            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                table: "SharedVideos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "SharedVideos");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 4, 10, 15, 35, 25, 266, DateTimeKind.Utc).AddTicks(7205),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4777));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 4, 10, 15, 35, 25, 266, DateTimeKind.Utc).AddTicks(6984),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 4, 10, 16, 41, 40, 94, DateTimeKind.Utc).AddTicks(4544));
        }
    }
}
