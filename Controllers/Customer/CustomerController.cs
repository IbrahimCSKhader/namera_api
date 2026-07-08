using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Customer;
using namera_API.Services.Customer;

namespace namera_API.Controllers.Customer;

[ApiController]
[Authorize(Roles = AppRoles.Customer)]
[Route("api/customer")]
public sealed class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<CustomerProfileResponseDto>>> GetProfile()
    {
        var response = await _customerService.GetProfileAsync(User);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<CustomerProfileResponseDto>>> UpdateProfile(UpdateCustomerProfileRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CustomerProfileResponseDto>.Fail("البيانات غير صالحة", GetModelErrors()));
        }

        var response = await _customerService.UpdateProfileAsync(User, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private IReadOnlyList<string> GetModelErrors()
    {
        return ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .ToList();
    }
}
