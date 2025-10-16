using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class FixComonAreaObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommonAreaObjects_MaintenanceRequests_CommonAreaObjectId",
                table: "CommonAreaObjects");

            migrationBuilder.AlterColumn<int>(
                name: "CommonAreaObjectId",
                table: "CommonAreaObjects",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_CommonAreaObjectId",
                table: "MaintenanceRequests",
                column: "CommonAreaObjectId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_CommonAreaObjects_CommonAreaObjectId",
                table: "MaintenanceRequests",
                column: "CommonAreaObjectId",
                principalTable: "CommonAreaObjects",
                principalColumn: "CommonAreaObjectId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_CommonAreaObjects_CommonAreaObjectId",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_CommonAreaObjectId",
                table: "MaintenanceRequests");

            migrationBuilder.AlterColumn<int>(
                name: "CommonAreaObjectId",
                table: "CommonAreaObjects",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_CommonAreaObjects_MaintenanceRequests_CommonAreaObjectId",
                table: "CommonAreaObjects",
                column: "CommonAreaObjectId",
                principalTable: "MaintenanceRequests",
                principalColumn: "MaintenanceRequestId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
