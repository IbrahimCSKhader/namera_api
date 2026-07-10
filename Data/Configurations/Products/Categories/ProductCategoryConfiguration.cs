using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Products.Categories;

namespace namera_API.Data.Configurations.Products.Categories;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(category => category.Slug)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(category => category.Description)
            .HasMaxLength(2000);

        builder.Property(category => category.ImageUrl)
            .HasMaxLength(1000);

        builder.HasIndex(category => category.Slug)
            .IsUnique()
            .HasDatabaseName("IX_ProductCategories_Slug");

        builder.HasIndex(category => category.ParentCategoryId)
            .HasDatabaseName("IX_ProductCategories_ParentCategoryId");

        builder.HasIndex(category => category.IsActive)
            .HasDatabaseName("IX_ProductCategories_IsActive");

        builder.HasOne(category => category.ParentCategory)
            .WithMany(category => category.Subcategories)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
