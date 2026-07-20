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

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<CustomerDashboardResponseDto>>> GetDashboard()
    {
        var response = await _customerService.GetDashboardAsync(User);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerAddressResponseDto>>>> GetAddresses()
    {
        var response = await _customerService.GetAddressesAsync(User);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> CreateAddress(CustomerAddressRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CustomerAddressResponseDto>.Fail("البيانات غير صالحة", GetModelErrors()));
        }

        var response = await _customerService.CreateAddressAsync(User, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> UpdateAddress(Guid addressId, CustomerAddressRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CustomerAddressResponseDto>.Fail("البيانات غير صالحة", GetModelErrors()));
        }

        var response = await _customerService.UpdateAddressAsync(User, addressId, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAddress(Guid addressId)
    {
        var response = await _customerService.DeleteAddressAsync(User, addressId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerReviewResponseDto>>>> GetReviews()
    {
        var response = await _customerService.GetReviewsAsync(User);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost("reviews")]
    public async Task<ActionResult<ApiResponse<CustomerReviewResponseDto>>> SaveReview(CustomerReviewRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CustomerReviewResponseDto>.Fail("البيانات غير صالحة", GetModelErrors()));
        }

        var response = await _customerService.SaveReviewAsync(User, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("reviews/{reviewId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteReview(Guid reviewId)
    {
        var response = await _customerService.DeleteReviewAsync(User, reviewId);
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
