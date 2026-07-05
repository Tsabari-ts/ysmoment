using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YsMoment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersEventPhoneIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_EventId_Phone",
                table: "Orders",
                columns: new[] { "EventId", "Phone" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_EventId_Phone",
                table: "Orders");
        }
    }
}
