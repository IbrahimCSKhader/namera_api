using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Orders;
using namera_API.DTOs.Products;
using namera_API.Services.Orders;
using namera_API.Services.Products;

namespace namera_API.Controllers.Orders;

[ApiController]
[Authorize(Roles = AppRoles.Customer)]
[Route("api/customer/orders")]
public sealed class CustomerOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IProductMediaStorageService _productMediaStorageService;

    public CustomerOrdersController(IOrderService orderService, IProductMediaStorageService productMediaStorageService)
    {
        _orderService = orderService;
        _productMediaStorageService = productMediaStorageService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderResponseDto>>>> GetMyOrders()
    {
        var response = await _orderService.GetCustomerOrdersAsync(User);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> CreateOrder(CreateOrderRequestDto request)
    {
        var response = await _orderService.CreateOrderAsync(User, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("uploads/customization-image")]
    [AllowAnonymous]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadedMediaDto>>> UploadCustomizationImage([FromForm] UploadOrderCustomizationImageRequestDto request)
    {
        var response = await _productMediaStorageService.SaveOrderCustomizationImageAsync(request.File, HttpContext.RequestAborted);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
