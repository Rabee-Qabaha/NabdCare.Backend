using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabdCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRateMarkup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExchangeRate",
                table: "Payments",
                newName: "FinalExchangeRate");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseExchangeRate",
                table: "Payments",
                type: "numeric(18,6)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseExchangeRate",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "FinalExchangeRate",
                table: "Payments",
                newName: "ExchangeRate");
        }
    }
}
