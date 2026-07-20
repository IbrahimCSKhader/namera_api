using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Owner;
using namera_API.Services.Owner;

namespace namera_API.Controllers.Owner;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/admin/account")]
public sealed class AdminAccountController : ControllerBase
{
    private readonly IOwnerAccountService _ownerAccountService;

    public AdminAccountController(IOwnerAccountService ownerAccountService)
    {
        _ownerAccountService = ownerAccountService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<OwnerProfileResponseDto>>> GetProfile()
    {
        var response = await _ownerAccountService.GetProfileAsync(User);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<OwnerProfileResponseDto>>> UpdateProfile(UpdateOwnerProfileRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<OwnerProfileResponseDto>.Fail("البيانات غير صالحة", GetModelErrors()));
        }

        var response = await _ownerAccountService.UpdateProfileAsync(User, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(ChangeOwnerPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.Fail("البيانات غير صالحة", GetModelErrors()));
        }

        var response = await _ownerAccountService.ChangePasswordAsync(User, request);
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
