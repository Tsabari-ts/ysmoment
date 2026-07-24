using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YsMoment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUploadFailedAndMuteNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ImageUploadFailed",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MuteCustomerNotifications",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUploadFailed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MuteCustomerNotifications",
                table: "Events");
        }
    }
}
