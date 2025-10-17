using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkSlots_Slot_SlotId",
                table: "WorkSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Slot",
                table: "Slot");

            migrationBuilder.RenameTable(
                name: "Slot",
                newName: "Slots");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Slots",
                table: "Slots",
                column: "SlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSlots_Slots_SlotId",
                table: "WorkSlots",
                column: "SlotId",
                principalTable: "Slots",
                principalColumn: "SlotId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkSlots_Slots_SlotId",
                table: "WorkSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Slots",
                table: "Slots");

            migrationBuilder.RenameTable(
                name: "Slots",
                newName: "Slot");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Slot",
                table: "Slot",
                column: "SlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSlots_Slot_SlotId",
                table: "WorkSlots",
                column: "SlotId",
                principalTable: "Slot",
                principalColumn: "SlotId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
