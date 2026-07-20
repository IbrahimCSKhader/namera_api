using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Owner;
using namera_API.Services.Owner;

namespace namera_API.Controllers.Owner;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/admin/settings")]
public sealed class AdminSettingsController : ControllerBase
{
    private readonly IOwnerAccountService _ownerAccountService;

    public AdminSettingsController(IOwnerAccountService ownerAccountService)
    {
        _ownerAccountService = ownerAccountService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<StoreSettingsDto>>> GetSettings()
    {
        return Ok(await _ownerAccountService.GetSettingsAsync());
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<StoreSettingsDto>>> UpdateSettings(UpdateStoreSettingsRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<StoreSettingsDto>.Fail("البيانات غير صالحة", GetModelErrors()));
        }

        var response = await _ownerAccountService.UpdateSettingsAsync(request);
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
