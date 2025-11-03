using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ApprovelReportEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportApprovals_InspectionReports_InspectionReportId",
                table: "ReportApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportApprovals_RepairReports_RepairReportId",
                table: "ReportApprovals");

            migrationBuilder.AlterColumn<int>(
                name: "RepairReportId",
                table: "ReportApprovals",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "InspectionReportId",
                table: "ReportApprovals",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportApprovals_InspectionReports_InspectionReportId",
                table: "ReportApprovals",
                column: "InspectionReportId",
                principalTable: "InspectionReports",
                principalColumn: "InspectionReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportApprovals_RepairReports_RepairReportId",
                table: "ReportApprovals",
                column: "RepairReportId",
                principalTable: "RepairReports",
                principalColumn: "RepairReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportApprovals_InspectionReports_InspectionReportId",
                table: "ReportApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportApprovals_RepairReports_RepairReportId",
                table: "ReportApprovals");

            migrationBuilder.AlterColumn<int>(
                name: "RepairReportId",
                table: "ReportApprovals",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InspectionReportId",
                table: "ReportApprovals",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportApprovals_InspectionReports_InspectionReportId",
                table: "ReportApprovals",
                column: "InspectionReportId",
                principalTable: "InspectionReports",
                principalColumn: "InspectionReportId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportApprovals_RepairReports_RepairReportId",
                table: "ReportApprovals",
                column: "RepairReportId",
                principalTable: "RepairReports",
                principalColumn: "RepairReportId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
