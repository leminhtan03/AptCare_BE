using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvoiceAccessoryUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceAccessories_InvoiceId",
                table: "InvoiceAccessories");

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "InvoiceAccessories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAccessories_InvoiceId_AccessoryId_SourceType",
                table: "InvoiceAccessories",
                columns: new[] { "InvoiceId", "AccessoryId", "SourceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceAccessories_InvoiceId_AccessoryId_SourceType",
                table: "InvoiceAccessories");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "InvoiceAccessories");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAccessories_InvoiceId",
                table: "InvoiceAccessories",
                column: "InvoiceId");
        }
    }
}
