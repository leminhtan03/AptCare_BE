using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class Technique_CommonAO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_Techniques_RequiredTechniqueId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceSchedules_RequiredTechniqueId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "RequiredTechniqueId",
                table: "MaintenanceSchedules");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "RequiredTechniqueId",
                table: "MaintenanceSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_RequiredTechniqueId",
                table: "MaintenanceSchedules",
                column: "RequiredTechniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_Techniques_RequiredTechniqueId",
                table: "MaintenanceSchedules",
                column: "RequiredTechniqueId",
                principalTable: "Techniques",
                principalColumn: "TechniqueId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
