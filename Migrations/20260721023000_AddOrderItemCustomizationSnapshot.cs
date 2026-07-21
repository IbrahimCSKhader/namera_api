using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace namera_API.Migrations
{
    public partial class AddOrderItemCustomizationSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomizationDetailsJson",
                table: "OrderItems",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomizationSummary",
                table: "OrderItems",
                type: "nvarchar(1200)",
                maxLength: 1200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomizationDetailsJson",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CustomizationSummary",
                table: "OrderItems");
        }
    }
}
