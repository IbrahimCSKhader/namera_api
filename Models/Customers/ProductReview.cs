using namera_API.Models.Common;
using namera_API.Models.Identity;
using namera_API.Models.Products.Products;

namespace namera_API.Models.Customers;

public sealed class ProductReview : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public string? GuestPhoneNumber { get; set; }
    public bool IsVisible { get; set; } = true;

    public ApplicationUser? Customer { get; set; }
    public Product Product { get; set; } = null!;
}
