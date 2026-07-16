using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Products.Products;

namespace namera_API.Data.Configurations.Products.Products;

public sealed class ProductOptionGroupConfiguration : IEntityTypeConfiguration<ProductOptionGroup>
{
    public void Configure(EntityTypeBuilder<ProductOptionGroup> builder)
    {
        builder.ToTable("ProductOptionGroups");
        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(group => group.Description)
            .HasMaxLength(500);

        builder.HasIndex(group => new { group.ProductId, group.DisplayOrder })
            .HasDatabaseName("IX_ProductOptionGroups_ProductId_DisplayOrder");

        builder.HasOne(group => group.Product)
            .WithMany(product => product.OptionGroups)
            .HasForeignKey(group => group.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
