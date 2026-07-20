using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Orders;

namespace namera_API.Data.Configurations.Orders;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Subtotal_NonNegative", "[Subtotal] >= 0");
            table.HasCheckConstraint("CK_Orders_Total_NonNegative", "[Total] >= 0");
        });

        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(order => order.Subtotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(order => order.Total)
            .HasColumnType("decimal(18,2)");

        builder.Property(order => order.Currency)
            .IsRequired()
            .HasMaxLength(12);

        builder.Property(order => order.CustomerName)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(order => order.CustomerPhoneNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(order => order.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(order => order.Notes)
            .HasMaxLength(1000);

        builder.Property(order => order.OwnerNote)
            .HasMaxLength(1000);

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_Orders_OrderNumber");

        builder.HasIndex(order => order.CustomerId)
            .HasDatabaseName("IX_Orders_CustomerId");

        builder.HasIndex(order => order.Status)
            .HasDatabaseName("IX_Orders_Status");

        builder.HasOne(order => order.Customer)
            .WithMany()
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
