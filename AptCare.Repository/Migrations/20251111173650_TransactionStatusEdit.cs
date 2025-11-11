using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class TransactionStatusEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOSCheckoutUrl",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PayOSOrderCode",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOSTransactionId",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayOSCheckoutUrl",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayOSOrderCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayOSTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transactions");
        }
    }
}
