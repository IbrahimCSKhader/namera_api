using namera_API.Models.Common;

namespace namera_API.Models.Store;

public sealed class StoreSettings : BaseEntity
{
    public string StoreName { get; set; } = "Resin Bon";
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string InstagramUrl { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "ILS";
    public string AboutText { get; set; } = string.Empty;
    public bool OrdersEnabled { get; set; } = true;
}
