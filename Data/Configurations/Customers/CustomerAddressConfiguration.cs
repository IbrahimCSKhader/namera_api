using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Customers;

namespace namera_API.Data.Configurations.Customers;

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");

        builder.Property(address => address.Label)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(address => address.RecipientName)
            .IsRequired()
            .HasMaxLength(160);

        builder.Property(address => address.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(address => address.AddressLine)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(address => address.City)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(address => address.Notes)
            .HasMaxLength(500);

        builder.HasIndex(address => new { address.CustomerId, address.IsDefault })
            .HasDatabaseName("IX_CustomerAddresses_CustomerId_IsDefault");

        builder.HasOne(address => address.Customer)
            .WithMany()
            .HasForeignKey(address => address.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
