using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Orders;

namespace namera_API.Data.Configurations.Orders;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] >= 1");
            table.HasCheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_OrderItems_LineTotal_NonNegative", "[LineTotal] >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductName)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(item => item.ProductSlug)
            .IsRequired()
            .HasMaxLength(240);

        builder.Property(item => item.CategoryName)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(item => item.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(item => item.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(item => item.LineTotal)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(item => item.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId");

        builder.HasIndex(item => item.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductId");

        builder.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
