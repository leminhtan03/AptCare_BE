using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddNullPossibleForInvoiceAccessory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceAccessories_Accessories_AccessoryId",
                table: "InvoiceAccessories");

            migrationBuilder.AlterColumn<int>(
                name: "AccessoryId",
                table: "InvoiceAccessories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceAccessories_Accessories_AccessoryId",
                table: "InvoiceAccessories",
                column: "AccessoryId",
                principalTable: "Accessories",
                principalColumn: "AccessoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceAccessories_Accessories_AccessoryId",
                table: "InvoiceAccessories");

            migrationBuilder.AlterColumn<int>(
                name: "AccessoryId",
                table: "InvoiceAccessories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceAccessories_Accessories_AccessoryId",
                table: "InvoiceAccessories",
                column: "AccessoryId",
                principalTable: "Accessories",
                principalColumn: "AccessoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
