using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Products.Products;

namespace namera_API.Data.Configurations.Products.Products;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(image => image.AltText)
            .HasMaxLength(250);

        builder.HasIndex(image => image.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");

        builder.HasIndex(image => new { image.ProductId, image.DisplayOrder })
            .HasDatabaseName("IX_ProductImages_ProductId_DisplayOrder");

        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
