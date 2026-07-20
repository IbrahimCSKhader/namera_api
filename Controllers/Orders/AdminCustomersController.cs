using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Orders;
using namera_API.Services.Orders;

namespace namera_API.Controllers.Orders;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/admin/customers")]
public sealed class AdminCustomersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminCustomersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OwnerCustomerResponseDto>>>> GetCustomers()
    {
        return Ok(await _orderService.GetCustomersAsync());
    }
}
