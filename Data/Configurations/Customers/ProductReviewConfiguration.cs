using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Customers;

namespace namera_API.Data.Configurations.Customers;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews", table =>
        {
            table.HasCheckConstraint("CK_ProductReviews_Rating_Range", "[Rating] >= 1 AND [Rating] <= 5");
        });

        builder.Property(review => review.Comment)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(review => new { review.CustomerId, review.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_ProductReviews_CustomerId_ProductId");

        builder.HasIndex(review => review.ProductId)
            .HasDatabaseName("IX_ProductReviews_ProductId");

        builder.HasOne(review => review.Customer)
            .WithMany()
            .HasForeignKey(review => review.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(review => review.Product)
            .WithMany()
            .HasForeignKey(review => review.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
