using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugInConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Conversations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Conversations");
        }
    }
}
