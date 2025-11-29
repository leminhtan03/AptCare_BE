using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CommonAreaObjectEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommonAreaObjects_Techniques_TechniqueId",
                table: "CommonAreaObjects");

            migrationBuilder.DropIndex(
                name: "IX_CommonAreaObjects_TechniqueId",
                table: "CommonAreaObjects");

            migrationBuilder.DropColumn(
                name: "TechniqueId",
                table: "CommonAreaObjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TechniqueId",
                table: "CommonAreaObjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommonAreaObjects_TechniqueId",
                table: "CommonAreaObjects",
                column: "TechniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommonAreaObjects_Techniques_TechniqueId",
                table: "CommonAreaObjects",
                column: "TechniqueId",
                principalTable: "Techniques",
                principalColumn: "TechniqueId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
