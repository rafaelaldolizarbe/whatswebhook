using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppWebhook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedsHumanAttention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsHumanAttention",
                table: "Contacts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedsHumanAttention",
                table: "Contacts");
        }
    }
}
