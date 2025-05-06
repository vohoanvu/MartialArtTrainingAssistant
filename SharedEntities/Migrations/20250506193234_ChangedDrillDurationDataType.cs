using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class ChangedDrillDurationDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(930),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 6, 13, 35, 40, 756, DateTimeKind.Utc).AddTicks(7370));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(340),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 6, 13, 35, 40, 756, DateTimeKind.Utc).AddTicks(6860));

            migrationBuilder.AlterColumn<string>(
                name: "Duration",
                table: "Drills",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 6, 13, 35, 40, 756, DateTimeKind.Utc).AddTicks(7370),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(930));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 6, 13, 35, 40, 756, DateTimeKind.Utc).AddTicks(6860),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 6, 19, 32, 33, 644, DateTimeKind.Utc).AddTicks(340));

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "Drills",
                type: "interval",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
