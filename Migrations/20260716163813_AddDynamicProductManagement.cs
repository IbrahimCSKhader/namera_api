using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace namera_API.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicProductManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowBackorder",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowOrdering",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowRatings",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Products",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "DirectAccessOnly",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasVariants",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InventoryTrackingEnabled",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriceVisible",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MadeToOrder",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxPreparationDays",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinPreparationDays",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreparationNote",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreparationUnit",
                table: "Products",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PriceLabel",
                table: "Products",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricingType",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInSuggestions",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnHomepage",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VisibleFrom",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VisibleTo",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductCustomizationFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    AdditionalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinLength = table.Column<int>(type: "int", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowedFilesCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCustomizationFields", x => x.Id);
                    table.CheckConstraint("CK_ProductCustomizationFields_AdditionalPrice_NonNegative", "[AdditionalPrice] >= 0");
                    table.CheckConstraint("CK_ProductCustomizationFields_LengthRange_Valid", "[MaxLength] IS NULL OR [MinLength] IS NULL OR [MaxLength] >= [MinLength]");
                    table.CheckConstraint("CK_ProductCustomizationFields_ValueRange_Valid", "[MaxValue] IS NULL OR [MinValue] IS NULL OR [MaxValue] >= [MinValue]");
                    table.ForeignKey(
                        name: "FK_ProductCustomizationFields_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptionGroups_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCustomizationChoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCustomizationFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AdditionalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCustomizationChoices", x => x.Id);
                    table.CheckConstraint("CK_ProductCustomizationChoices_AdditionalPrice_NonNegative", "[AdditionalPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductCustomizationChoices_ProductCustomizationFields_ProductCustomizationFieldId",
                        column: x => x.ProductCustomizationFieldId,
                        principalTable: "ProductCustomizationFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductOptionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExtraPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionValues", x => x.Id);
                    table.CheckConstraint("CK_ProductOptionValues_ExtraPrice_NonNegative", "[ExtraPrice] >= 0");
                    table.CheckConstraint("CK_ProductOptionValues_StockQuantity_NonNegative", "[StockQuantity] IS NULL OR [StockQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductOptionValues_ProductOptionGroups_ProductOptionGroupId",
                        column: x => x.ProductOptionGroupId,
                        principalTable: "ProductOptionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ShowOnHomepage",
                table: "Products",
                column: "ShowOnHomepage");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_LowStockThreshold_NonNegative",
                table: "Products",
                sql: "[LowStockThreshold] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_PreparationRange_Valid",
                table: "Products",
                sql: "[MaxPreparationDays] IS NULL OR [MinPreparationDays] IS NULL OR [MaxPreparationDays] >= [MinPreparationDays]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Quantity_NonNegative",
                table: "Products",
                sql: "[Quantity] IS NULL OR [Quantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCustomizationChoices_FieldId_DisplayOrder",
                table: "ProductCustomizationChoices",
                columns: new[] { "ProductCustomizationFieldId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCustomizationFields_ProductId_DisplayOrder",
                table: "ProductCustomizationFields",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionGroups_ProductId_DisplayOrder",
                table: "ProductOptionGroups",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_GroupId_DisplayOrder",
                table: "ProductOptionValues",
                columns: new[] { "ProductOptionGroupId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCustomizationChoices");

            migrationBuilder.DropTable(
                name: "ProductOptionValues");

            migrationBuilder.DropTable(
                name: "ProductCustomizationFields");

            migrationBuilder.DropTable(
                name: "ProductOptionGroups");

            migrationBuilder.DropIndex(
                name: "IX_Products_ShowOnHomepage",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_LowStockThreshold_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_PreparationRange_Valid",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Quantity_NonNegative",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AllowBackorder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AllowOrdering",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AllowRatings",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DirectAccessOnly",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasVariants",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "InventoryTrackingEnabled",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsPriceVisible",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MadeToOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxPreparationDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinPreparationDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PreparationNote",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PreparationUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PriceLabel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PricingType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShowInSuggestions",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShowOnHomepage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VisibleFrom",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VisibleTo",
                table: "Products");
        }
    }
}
