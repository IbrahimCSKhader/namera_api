using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using namera_API.Models.Store;

namespace namera_API.Data.Configurations.Store;

public sealed class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("StoreSettings");

        builder.Property(settings => settings.StoreName)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(settings => settings.ContactPhone)
            .HasMaxLength(20);

        builder.Property(settings => settings.ContactEmail)
            .HasMaxLength(256);

        builder.Property(settings => settings.InstagramUrl)
            .HasMaxLength(300);

        builder.Property(settings => settings.DefaultCurrency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(settings => settings.AboutText)
            .HasMaxLength(1200);
    }
}
