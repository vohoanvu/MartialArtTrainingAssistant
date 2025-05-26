using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharedEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitListTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 26, 10, 33, 11, 486, DateTimeKind.Utc).AddTicks(6320),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(8150));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 26, 10, 33, 11, 486, DateTimeKind.Utc).AddTicks(5760),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.CreateTable(
                name: "Waitlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waitlists", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Waitlists");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(8150),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 26, 10, 33, 11, 486, DateTimeKind.Utc).AddTicks(6320));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 15, 17, 57, 47, 757, DateTimeKind.Utc).AddTicks(7680),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 26, 10, 33, 11, 486, DateTimeKind.Utc).AddTicks(5760));
        }
    }
}
