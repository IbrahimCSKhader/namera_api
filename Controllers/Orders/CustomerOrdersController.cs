using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Orders;
using namera_API.Services.Orders;

namespace namera_API.Controllers.Orders;

[ApiController]
[Authorize(Roles = AppRoles.Customer)]
[Route("api/customer/orders")]
public sealed class CustomerOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public CustomerOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
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
}
