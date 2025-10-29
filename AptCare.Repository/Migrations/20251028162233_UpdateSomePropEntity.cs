using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSomePropEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingCode",
                table: "Floors");

            migrationBuilder.RenameColumn(
                name: "RoomNumber",
                table: "Apartments",
                newName: "Room");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserApartments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DisableAt",
                table: "UserApartments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<double>(
                name: "Area",
                table: "Apartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Limit",
                table: "Apartments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserApartments");

            migrationBuilder.DropColumn(
                name: "DisableAt",
                table: "UserApartments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Limit",
                table: "Apartments");

            migrationBuilder.RenameColumn(
                name: "Room",
                table: "Apartments",
                newName: "RoomNumber");

            migrationBuilder.AddColumn<string>(
                name: "BuildingCode",
                table: "Floors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
