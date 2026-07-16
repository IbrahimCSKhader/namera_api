using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using namera_API.Models.Identity;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Products;

namespace namera_API.Data;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductOptionGroup> ProductOptionGroups => Set<ProductOptionGroup>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductCustomizationField> ProductCustomizationFields => Set<ProductCustomizationField>();
    public DbSet<ProductCustomizationChoice> ProductCustomizationChoices => Set<ProductCustomizationChoice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(user => user.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(user => user.Address)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(user => user.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(user => user.PhoneNumber)
                .IsUnique();
        });
    }
}
