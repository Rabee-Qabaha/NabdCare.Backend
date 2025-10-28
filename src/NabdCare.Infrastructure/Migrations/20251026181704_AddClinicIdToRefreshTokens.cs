using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabdCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicIdToRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedForUserAgent",
                table: "RefreshTokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ClinicId",
                table: "RefreshTokens",
                column: "ClinicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ClinicId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IssuedForUserAgent",
                table: "RefreshTokens");
        }
    }
}
