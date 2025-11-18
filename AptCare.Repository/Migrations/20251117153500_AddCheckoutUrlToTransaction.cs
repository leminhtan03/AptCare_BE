using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutUrlToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Transactions");

            migrationBuilder.RenameColumn(
               name: "PayOSCheckoutUrl",
               table: "Transactions",
               newName: "CheckoutUrl"
           );
            migrationBuilder.RenameColumn(
               name: "PayOSOrderCode",
               table: "Transactions",
               newName: "OrderCode"
           );

        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameColumn(
               name: "CheckoutUrl",
               table: "Transactions",
               newName: "PayOSCheckoutUrl"
           );
            migrationBuilder.RenameColumn(
               name: "OrderCode",
               table: "Transactions",
               newName: "PayOSOrderCode"
           );
        }
    }
}
