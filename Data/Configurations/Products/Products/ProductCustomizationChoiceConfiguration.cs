using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Products.Products;

namespace namera_API.Data.Configurations.Products.Products;

public sealed class ProductCustomizationChoiceConfiguration : IEntityTypeConfiguration<ProductCustomizationChoice>
{
    public void Configure(EntityTypeBuilder<ProductCustomizationChoice> builder)
    {
        builder.ToTable("ProductCustomizationChoices", table =>
        {
            table.HasCheckConstraint("CK_ProductCustomizationChoices_AdditionalPrice_NonNegative", "[AdditionalPrice] >= 0");
        });

        builder.HasKey(choice => choice.Id);

        builder.Property(choice => choice.Label)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(choice => choice.AdditionalPrice)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(choice => new { choice.ProductCustomizationFieldId, choice.DisplayOrder })
            .HasDatabaseName("IX_ProductCustomizationChoices_FieldId_DisplayOrder");

        builder.HasOne(choice => choice.ProductCustomizationField)
            .WithMany(field => field.Choices)
            .HasForeignKey(choice => choice.ProductCustomizationFieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
