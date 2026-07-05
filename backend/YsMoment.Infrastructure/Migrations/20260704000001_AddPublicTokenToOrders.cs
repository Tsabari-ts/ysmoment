using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YsMoment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicTokenToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicToken",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PublicToken",
                table: "Orders",
                column: "PublicToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PublicToken",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PublicToken",
                table: "Orders");
        }
    }
}
