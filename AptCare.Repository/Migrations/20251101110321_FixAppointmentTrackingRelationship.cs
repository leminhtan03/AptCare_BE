using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class FixAppointmentTrackingRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RequestTrackings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppointmentTrackings",
                columns: table => new
                {
                    TrackingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTrackings", x => x.TrackingId);
                    table.ForeignKey(
                        name: "FK_AppointmentTrackings_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentTrackings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTrackings_UserId",
                table: "RequestTrackings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTrackings_AppointmentId",
                table: "AppointmentTrackings",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTrackings_UpdatedBy",
                table: "AppointmentTrackings",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTrackings_Users_UserId",
                table: "RequestTrackings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestTrackings_Users_UserId",
                table: "RequestTrackings");

            migrationBuilder.DropTable(
                name: "AppointmentTrackings");

            migrationBuilder.DropIndex(
                name: "IX_RequestTrackings_UserId",
                table: "RequestTrackings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RequestTrackings");
        }
    }
}
