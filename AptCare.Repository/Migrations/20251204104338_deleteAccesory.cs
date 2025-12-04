using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class deleteAccesory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceAccessories_Accessories_AccessoryId",
                table: "InvoiceAccessories");

            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceAccessories_AccessoryId",
                table: "InvoiceAccessories");

            migrationBuilder.DropColumn(
                name: "AccessoryId",
                table: "InvoiceAccessories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessoryId",
                table: "InvoiceAccessories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    AccessoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descrption = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.AccessoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAccessories_AccessoryId",
                table: "InvoiceAccessories",
                column: "AccessoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceAccessories_Accessories_AccessoryId",
                table: "InvoiceAccessories",
                column: "AccessoryId",
                principalTable: "Accessories",
                principalColumn: "AccessoryId");
        }
    }
}
