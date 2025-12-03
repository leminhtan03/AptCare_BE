using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityRelateMaintenance : Migration
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

            migrationBuilder.AddColumn<int>(
                name: "CommonAreaObjectTypeId",
                table: "CommonAreaObjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    BudgetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.BudgetId);
                });

            migrationBuilder.CreateTable(
                name: "CommonAreaObjectTypes",
                columns: table => new
                {
                    CommonAreaObjectTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommonAreaObjectTypes", x => x.CommonAreaObjectTypeId);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTasks",
                columns: table => new
                {
                    MaintenanceTaskId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommonAreaObjectTypeId = table.Column<int>(type: "integer", nullable: false),
                    TaskName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TaskDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequiredTools = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    EstimatedDurationMinutes = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTasks", x => x.MaintenanceTaskId);
                    table.ForeignKey(
                        name: "FK_MaintenanceTasks_CommonAreaObjectTypes_CommonAreaObjectType~",
                        column: x => x.CommonAreaObjectTypeId,
                        principalTable: "CommonAreaObjectTypes",
                        principalColumn: "CommonAreaObjectTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairRequestTasks",
                columns: table => new
                {
                    RepairRequestTaskId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepairRequestId = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceTaskTemplateId = table.Column<int>(type: "integer", nullable: true),
                    TaskName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TaskDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TechnicianNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectionResult = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairRequestTasks", x => x.RepairRequestTaskId);
                    table.ForeignKey(
                        name: "FK_RepairRequestTasks_MaintenanceTasks_MaintenanceTaskTemplate~",
                        column: x => x.MaintenanceTaskTemplateId,
                        principalTable: "MaintenanceTasks",
                        principalColumn: "MaintenanceTaskId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RepairRequestTasks_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RepairRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepairRequestTasks_Users_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommonAreaObjects_CommonAreaObjectTypeId",
                table: "CommonAreaObjects",
                column: "CommonAreaObjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTasks_CommonAreaObjectTypeId",
                table: "MaintenanceTasks",
                column: "CommonAreaObjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequestTasks_CompletedByUserId",
                table: "RepairRequestTasks",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequestTasks_MaintenanceTaskTemplateId",
                table: "RepairRequestTasks",
                column: "MaintenanceTaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequestTasks_RepairRequestId",
                table: "RepairRequestTasks",
                column: "RepairRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommonAreaObjects_CommonAreaObjectTypes_CommonAreaObjectTyp~",
                table: "CommonAreaObjects",
                column: "CommonAreaObjectTypeId",
                principalTable: "CommonAreaObjectTypes",
                principalColumn: "CommonAreaObjectTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommonAreaObjects_CommonAreaObjectTypes_CommonAreaObjectTyp~",
                table: "CommonAreaObjects");

            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropTable(
                name: "RepairRequestTasks");

            migrationBuilder.DropTable(
                name: "MaintenanceTasks");

            migrationBuilder.DropTable(
                name: "CommonAreaObjectTypes");

            migrationBuilder.DropIndex(
                name: "IX_CommonAreaObjects_CommonAreaObjectTypeId",
                table: "CommonAreaObjects");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CommonAreaObjectTypeId",
                table: "CommonAreaObjects");
        }
    }
}
