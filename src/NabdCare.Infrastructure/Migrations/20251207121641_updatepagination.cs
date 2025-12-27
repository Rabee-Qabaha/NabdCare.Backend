using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabdCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatepagination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Clinics",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Slug",
                table: "Clinics",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clinics_Slug",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Clinics");
        }
    }
}
