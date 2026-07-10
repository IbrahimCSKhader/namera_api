using namera_API.Common.Responses;
using namera_API.DTOs.Products;

namespace namera_API.Services.Products;

public interface IProductService
{
    Task<ApiResponse<IReadOnlyList<ProductResponseDto>>> GetProductsAsync();
    Task<ApiResponse<IReadOnlyList<ProductCategoryResponseDto>>> GetCategoriesAsync();
}
