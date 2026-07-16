using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Products;
using namera_API.Services.Products;

namespace namera_API.Controllers.Products;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/admin/products")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IProductManagementService _productManagementService;

    public AdminProductsController(IProductManagementService productManagementService)
    {
        _productManagementService = productManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminProductListItemDto>>>> GetProducts([FromQuery] AdminProductListQueryDto query)
    {
        return Ok(await _productManagementService.GetProductsAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailsDto>>> GetProduct(Guid id)
    {
        var response = await _productManagementService.GetProductAsync(id);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDetailsDto>>> CreateProduct(CreateProductRequestDto request)
    {
        var response = await _productManagementService.CreateProductAsync(request);
        return response.Success ? CreatedAtAction(nameof(GetProduct), new { id = response.Data!.Id }, response) : BadRequest(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailsDto>>> UpdateProduct(Guid id, UpdateProductRequestDto request)
    {
        var response = await _productManagementService.UpdateProductAsync(id, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<ProductDetailsDto>>> PublishProduct(Guid id, [FromQuery] bool publish = true)
    {
        var response = await _productManagementService.PublishProductAsync(id, publish);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:guid}/archive")]
    public async Task<ActionResult<ApiResponse<ProductDetailsDto>>> ArchiveProduct(Guid id)
    {
        var response = await _productManagementService.ArchiveProductAsync(id);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<ProductCategoryResponseDto>>> CreateCategory(CreateProductCategoryRequestDto request)
    {
        var response = await _productManagementService.CreateCategoryAsync(request);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
