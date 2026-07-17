using namera_API.Common.Responses;
using namera_API.DTOs.Products;

namespace namera_API.Services.Products;

public sealed class ProductMediaStorageService : IProductMediaStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private const long MaxImageBytes = 8 * 1024 * 1024;
    private readonly IWebHostEnvironment _environment;

    public ProductMediaStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<ApiResponse<UploadedMediaDto>> SaveProductImageAsync(Guid productId, IFormFile? file, CancellationToken cancellationToken = default)
    {
        return SaveImageAsync(file, ["uploads", "products", productId.ToString("N"), "images"], cancellationToken);
    }

    public Task<ApiResponse<UploadedMediaDto>> SaveCategoryImageAsync(Guid categoryId, IFormFile? file, CancellationToken cancellationToken = default)
    {
        return SaveImageAsync(file, ["uploads", "categories", categoryId.ToString("N"), "cover"], cancellationToken);
    }

    private async Task<ApiResponse<UploadedMediaDto>> SaveImageAsync(IFormFile? file, IReadOnlyList<string> relativeSegments, CancellationToken cancellationToken)
    {
        var errors = ValidateFile(file);
        if (errors.Count > 0)
        {
            return ApiResponse<UploadedMediaDto>.Fail("راجع ملف الصورة قبل الرفع", errors);
        }

        var webRoot = EnsureWebRoot();
        var directory = relativeSegments.Aggregate(webRoot, Path.Combine);
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(file!.FileName).ToLowerInvariant();
        var storedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(directory, storedFileName);

        await using (var stream = File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var urlSegments = relativeSegments.Append(storedFileName).Select(Uri.EscapeDataString);
        var url = "/" + string.Join('/', urlSegments);

        return ApiResponse<UploadedMediaDto>.Ok(new UploadedMediaDto
        {
            Url = url,
            FileName = storedFileName,
            Size = file.Length
        }, "تم رفع الصورة بنجاح");
    }

    private static List<string> ValidateFile(IFormFile? file)
    {
        var errors = new List<string>();
        if (file is null || file.Length == 0)
        {
            errors.Add("ملف الصورة مطلوب.");
            return errors;
        }

        if (file.Length > MaxImageBytes)
        {
            errors.Add("حجم الصورة يجب ألا يتجاوز 8 ميجابايت.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            errors.Add("الصيغ المسموحة هي JPG و PNG و WEBP و GIF.");
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("الملف المختار يجب أن يكون صورة.");
        }

        return errors;
    }

    private string EnsureWebRoot()
    {
        var webRoot = _environment.WebRootPath;
        if (!string.IsNullOrWhiteSpace(webRoot))
        {
            Directory.CreateDirectory(webRoot);
            return webRoot;
        }

        webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(webRoot);
        return webRoot;
    }
}
