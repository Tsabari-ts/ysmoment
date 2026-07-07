using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YsMoment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNotificationFailed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationFailed",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationFailed",
                table: "Orders");
        }
    }
}
