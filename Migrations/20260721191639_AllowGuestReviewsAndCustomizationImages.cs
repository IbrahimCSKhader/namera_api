using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace namera_API.Migrations
{
    /// <inheritdoc />
    public partial class AllowGuestReviewsAndCustomizationImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_AspNetUsers_CustomerId",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_CustomerId_ProductId",
                table: "ProductReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductReviews_Rating_Range",
                table: "ProductReviews");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "ProductReviews",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "ProductReviews",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhoneNumber",
                table: "ProductReviews",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_CustomerId_ProductId",
                table: "ProductReviews",
                columns: new[] { "CustomerId", "ProductId" },
                unique: true,
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductReviews_Rating_Range",
                table: "ProductReviews",
                sql: "[Rating] >= 0 AND [Rating] <= 6");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_AspNetUsers_CustomerId",
                table: "ProductReviews",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_AspNetUsers_CustomerId",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_CustomerId_ProductId",
                table: "ProductReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductReviews_Rating_Range",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "GuestPhoneNumber",
                table: "ProductReviews");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "ProductReviews",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_CustomerId_ProductId",
                table: "ProductReviews",
                columns: new[] { "CustomerId", "ProductId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductReviews_Rating_Range",
                table: "ProductReviews",
                sql: "[Rating] >= 1 AND [Rating] <= 5");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_AspNetUsers_CustomerId",
                table: "ProductReviews",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
