using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Products.Products;

namespace namera_API.Data.Configurations.Products.Products;

public sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("ProductOptionValues", table =>
        {
            table.HasCheckConstraint("CK_ProductOptionValues_ExtraPrice_NonNegative", "[ExtraPrice] >= 0");
            table.HasCheckConstraint("CK_ProductOptionValues_StockQuantity_NonNegative", "[StockQuantity] IS NULL OR [StockQuantity] >= 0");
        });

        builder.HasKey(value => value.Id);

        builder.Property(value => value.Label)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(value => value.ExtraPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(value => value.Sku)
            .HasMaxLength(80);

        builder.Property(value => value.ImageUrl)
            .HasMaxLength(1000);

        builder.HasIndex(value => new { value.ProductOptionGroupId, value.DisplayOrder })
            .HasDatabaseName("IX_ProductOptionValues_GroupId_DisplayOrder");

        builder.HasOne(value => value.ProductOptionGroup)
            .WithMany(group => group.Values)
            .HasForeignKey(value => value.ProductOptionGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
