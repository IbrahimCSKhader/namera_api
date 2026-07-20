using System.Security.Claims;
using namera_API.Common.Responses;
using namera_API.DTOs.Orders;

namespace namera_API.Services.Orders;

public interface IOrderService
{
    Task<ApiResponse<OrderResponseDto>> CreateOrderAsync(ClaimsPrincipal principal, CreateOrderRequestDto request);
    Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> GetCustomerOrdersAsync(ClaimsPrincipal principal);
    Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> GetOwnerOrdersAsync(OrderListQueryDto query);
    Task<ApiResponse<OrderResponseDto>> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusRequestDto request);
    Task<ApiResponse<IReadOnlyList<OwnerCustomerResponseDto>>> GetCustomersAsync();
    Task<ApiResponse<OwnerDashboardStatsDto>> GetDashboardStatsAsync();
}
