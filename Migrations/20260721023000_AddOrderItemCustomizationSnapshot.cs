using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace namera_API.Migrations
{
    public partial class AddOrderItemCustomizationSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.OrderItems', N'CustomizationDetailsJson') IS NULL
                BEGIN
                    ALTER TABLE [OrderItems] ADD [CustomizationDetailsJson] nvarchar(4000) NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.OrderItems', N'CustomizationSummary') IS NULL
                BEGIN
                    ALTER TABLE [OrderItems] ADD [CustomizationSummary] nvarchar(1200) NULL;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.OrderItems', N'CustomizationDetailsJson') IS NOT NULL
                BEGIN
                    ALTER TABLE [OrderItems] DROP COLUMN [CustomizationDetailsJson];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.OrderItems', N'CustomizationSummary') IS NOT NULL
                BEGIN
                    ALTER TABLE [OrderItems] DROP COLUMN [CustomizationSummary];
                END
                """);
        }
    }
}
