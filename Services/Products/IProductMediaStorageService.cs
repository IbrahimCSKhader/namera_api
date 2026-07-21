using namera_API.Common.Responses;
using namera_API.DTOs.Products;

namespace namera_API.Services.Products;

public interface IProductMediaStorageService
{
    Task<ApiResponse<UploadedMediaDto>> SaveProductImageAsync(Guid productId, IFormFile? file, CancellationToken cancellationToken = default);
    Task<ApiResponse<UploadedMediaDto>> SaveCategoryImageAsync(Guid categoryId, IFormFile? file, CancellationToken cancellationToken = default);
    Task<ApiResponse<UploadedMediaDto>> SaveOrderCustomizationImageAsync(IFormFile? file, CancellationToken cancellationToken = default);
}
