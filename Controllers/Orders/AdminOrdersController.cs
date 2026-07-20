using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Orders;
using namera_API.Services.Orders;

namespace namera_API.Controllers.Orders;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/admin/orders")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderResponseDto>>>> GetOrders([FromQuery] OrderListQueryDto query)
    {
        return Ok(await _orderService.GetOwnerOrdersAsync(query));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> UpdateStatus(Guid id, UpdateOrderStatusRequestDto request)
    {
        var response = await _orderService.UpdateOrderStatusAsync(id, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
