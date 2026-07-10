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

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(product => product.MinimumQuantity)
            .HasDefaultValue(1);

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

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
