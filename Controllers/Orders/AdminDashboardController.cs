using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Orders;
using namera_API.Services.Orders;

namespace namera_API.Controllers.Orders;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminDashboardController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<OwnerDashboardStatsDto>>> GetStats()
    {
        return Ok(await _orderService.GetDashboardStatsAsync());
    }
}
