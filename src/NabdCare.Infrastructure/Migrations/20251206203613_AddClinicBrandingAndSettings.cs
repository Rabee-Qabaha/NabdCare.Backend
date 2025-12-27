using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabdCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicBrandingAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clinics_Email",
                table: "Clinics");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Clinics",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Clinics",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Clinics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Settings",
                table: "Clinics",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Clinics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Clinics",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Email",
                table: "Clinics",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clinics_Email",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Settings",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Clinics");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Clinics",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Email",
                table: "Clinics",
                column: "Email");
        }
    }
}
