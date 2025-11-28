using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class TechniqueRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAt",
                table: "Transactions",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }
    }
}
