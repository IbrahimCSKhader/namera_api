using namera_API.Models.Common;
using namera_API.Models.Identity;

namespace namera_API.Models.Customers;

public sealed class CustomerAddress : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }

    public ApplicationUser Customer { get; set; } = null!;
}
