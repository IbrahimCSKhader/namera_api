using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.DTOs.Products;
using namera_API.Services.Products;

namespace namera_API.Controllers.Products;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductResponseDto>>>> GetProducts()
    {
        return Ok(await _productService.GetProductsAsync());
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductCategoryResponseDto>>>> GetCategories()
    {
        return Ok(await _productService.GetCategoriesAsync());
    }
}
