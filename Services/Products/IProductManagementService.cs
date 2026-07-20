using namera_API.Common.Responses;
using namera_API.DTOs.Products;

namespace namera_API.Services.Products;

public interface IProductManagementService
{
    Task<ApiResponse<IReadOnlyList<AdminProductListItemDto>>> GetProductsAsync(AdminProductListQueryDto query);
    Task<ApiResponse<ProductDetailsDto>> GetProductAsync(Guid id);
    Task<ApiResponse<ProductDetailsDto>> CreateProductAsync(CreateProductRequestDto request);
    Task<ApiResponse<ProductDetailsDto>> UpdateProductAsync(Guid id, UpdateProductRequestDto request);
    Task<ApiResponse<ProductDetailsDto>> ArchiveProductAsync(Guid id);
    Task<ApiResponse<ProductDetailsDto>> PublishProductAsync(Guid id, bool publish);
    Task<ApiResponse<IReadOnlyList<AdminProductCategoryDto>>> GetCategoriesAsync();
    Task<ApiResponse<ProductCategoryResponseDto>> CreateCategoryAsync(CreateProductCategoryRequestDto request);
    Task<ApiResponse<AdminProductCategoryDto>> UpdateCategoryAsync(Guid id, UpdateProductCategoryRequestDto request);
    Task<ApiResponse<AdminProductCategoryDto>> SetCategoryActiveAsync(Guid id, bool isActive);
}
