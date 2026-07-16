using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace namera_API.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDynamicProductDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PricingType",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Fixed",
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "PreparationUnit",
                table: "Products",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Days",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "LowStockThreshold",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPriceVisible",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "InventoryTrackingEnabled",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Products",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "ILS",
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowRatings",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "AllowOrdering",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.Sql("""
                UPDATE [Products]
                SET
                    [PricingType] = CASE WHEN [PricingType] = N'' THEN N'Fixed' ELSE [PricingType] END,
                    [PreparationUnit] = CASE WHEN [PreparationUnit] = N'' THEN N'Days' ELSE [PreparationUnit] END,
                    [Currency] = CASE WHEN [Currency] = N'' THEN N'ILS' ELSE [Currency] END,
                    [IsPriceVisible] = 1,
                    [InventoryTrackingEnabled] = 1,
                    [LowStockThreshold] = CASE WHEN [LowStockThreshold] = 0 THEN 3 ELSE [LowStockThreshold] END,
                    [AllowRatings] = 1,
                    [AllowOrdering] = 1
                WHERE [PricingType] = N''
                   OR [PreparationUnit] = N''
                   OR [Currency] = N''
                   OR [IsPriceVisible] = 0
                   OR [InventoryTrackingEnabled] = 0
                   OR [LowStockThreshold] = 0
                   OR [AllowRatings] = 0
                   OR [AllowOrdering] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PricingType",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldDefaultValue: "Fixed");

            migrationBuilder.AlterColumn<string>(
                name: "PreparationUnit",
                table: "Products",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Days");

            migrationBuilder.AlterColumn<int>(
                name: "LowStockThreshold",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 3);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPriceVisible",
                table: "Products",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "InventoryTrackingEnabled",
                table: "Products",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Products",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12,
                oldDefaultValue: "ILS");

            migrationBuilder.AlterColumn<bool>(
                name: "AllowRatings",
                table: "Products",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowOrdering",
                table: "Products",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
