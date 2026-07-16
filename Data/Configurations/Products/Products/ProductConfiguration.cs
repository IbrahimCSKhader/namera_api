using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;

namespace namera_API.Data.Configurations.Products.Products;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_BasePrice_NonNegative", "[BasePrice] >= 0");
            table.HasCheckConstraint("CK_Products_MinimumQuantity_Positive", "[MinimumQuantity] >= 1");
            table.HasCheckConstraint("CK_Products_MaximumQuantity_Positive", "[MaximumQuantity] IS NULL OR [MaximumQuantity] >= [MinimumQuantity]");
            table.HasCheckConstraint("CK_Products_PreparationTime_NonNegative", "[PreparationTimeInDays] >= 0");
            table.HasCheckConstraint("CK_Products_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
            table.HasCheckConstraint("CK_Products_LowStockThreshold_NonNegative", "[LowStockThreshold] >= 0");
            table.HasCheckConstraint("CK_Products_PreparationRange_Valid", "[MaxPreparationDays] IS NULL OR [MinPreparationDays] IS NULL OR [MaxPreparationDays] >= [MinPreparationDays]");
        });

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(product => product.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(product => product.ShortDescription)
            .HasMaxLength(500);

        builder.Property(product => product.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(product => product.BasePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(product => product.PricingType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(ProductPricingType.Fixed);

        builder.Property(product => product.PriceLabel)
            .HasMaxLength(160);

        builder.Property(product => product.Currency)
            .IsRequired()
            .HasMaxLength(12)
            .HasDefaultValue("ILS");

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(product => product.MinimumQuantity)
            .HasDefaultValue(1);

        builder.Property(product => product.IsPriceVisible)
            .HasDefaultValue(true);

        builder.Property(product => product.InventoryTrackingEnabled)
            .HasDefaultValue(true);

        builder.Property(product => product.LowStockThreshold)
            .HasDefaultValue(3);

        builder.Property(product => product.AllowRatings)
            .HasDefaultValue(true);

        builder.Property(product => product.AllowOrdering)
            .HasDefaultValue(true);

        builder.Property(product => product.PreparationUnit)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(ProductPreparationUnit.Days);

        builder.Property(product => product.PreparationNote)
            .HasMaxLength(500);

        builder.HasIndex(product => product.Slug)
            .IsUnique()
            .HasDatabaseName("IX_Products_Slug");

        builder.HasIndex(product => product.CategoryId)
            .HasDatabaseName("IX_Products_CategoryId");

        builder.HasIndex(product => product.Status)
            .HasDatabaseName("IX_Products_Status");

        builder.HasIndex(product => product.IsFeatured)
            .HasDatabaseName("IX_Products_IsFeatured");

        builder.HasIndex(product => new { product.Status, product.DisplayOrder })
            .HasDatabaseName("IX_Products_Status_DisplayOrder");

        builder.HasIndex(product => product.ShowOnHomepage)
            .HasDatabaseName("IX_Products_ShowOnHomepage");

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
