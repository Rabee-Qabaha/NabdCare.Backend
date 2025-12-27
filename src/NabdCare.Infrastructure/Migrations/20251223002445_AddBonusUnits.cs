using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabdCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BonusBranches",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BonusUsers",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonusBranches",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "BonusUsers",
                table: "Subscriptions");
        }
    }
}
