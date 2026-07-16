using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Products.Products;

namespace namera_API.Data.Configurations.Products.Products;

public sealed class ProductCustomizationFieldConfiguration : IEntityTypeConfiguration<ProductCustomizationField>
{
    public void Configure(EntityTypeBuilder<ProductCustomizationField> builder)
    {
        builder.ToTable("ProductCustomizationFields", table =>
        {
            table.HasCheckConstraint("CK_ProductCustomizationFields_AdditionalPrice_NonNegative", "[AdditionalPrice] >= 0");
            table.HasCheckConstraint("CK_ProductCustomizationFields_LengthRange_Valid", "[MaxLength] IS NULL OR [MinLength] IS NULL OR [MaxLength] >= [MinLength]");
            table.HasCheckConstraint("CK_ProductCustomizationFields_ValueRange_Valid", "[MaxValue] IS NULL OR [MinValue] IS NULL OR [MaxValue] >= [MinValue]");
        });

        builder.HasKey(field => field.Id);

        builder.Property(field => field.Label)
            .IsRequired()
            .HasMaxLength(140);

        builder.Property(field => field.FieldType)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(field => field.Description)
            .HasMaxLength(700);

        builder.Property(field => field.Placeholder)
            .HasMaxLength(180);

        builder.Property(field => field.AdditionalPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(field => field.MinValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(field => field.MaxValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(field => field.AllowedFilesCsv)
            .HasMaxLength(500);

        builder.HasIndex(field => new { field.ProductId, field.DisplayOrder })
            .HasDatabaseName("IX_ProductCustomizationFields_ProductId_DisplayOrder");

        builder.HasOne(field => field.Product)
            .WithMany(product => product.CustomizationFields)
            .HasForeignKey(field => field.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
