using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabdCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BonusUsers",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "BonusBranches",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingCycleAnchor",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CancelAtPeriodEnd",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Subscriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Subscriptions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<string>(
                name: "ExternalSubscriptionId",
                table: "Subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<string>(
                name: "HostedPaymentUrl",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextPaymentAttempt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentAttemptCount",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PdfUrl",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CancelAtPeriodEnd",
                table: "Subscriptions",
                column: "CancelAtPeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedAt",
                table: "Subscriptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalSubscriptionId",
                table: "Subscriptions",
                column: "ExternalSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Currency",
                table: "Invoices",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IdempotencyKey",
                table: "Invoices",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CancelAtPeriodEnd",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CreatedAt",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_ExternalSubscriptionId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Currency",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_IdempotencyKey",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillingCycleAnchor",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CancelAtPeriodEnd",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CanceledAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalSubscriptionId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "HostedPaymentUrl",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "NextPaymentAttempt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentAttemptCount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PdfUrl",
                table: "Invoices");

            migrationBuilder.AlterColumn<int>(
                name: "BonusUsers",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "BonusBranches",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }
    }
}
