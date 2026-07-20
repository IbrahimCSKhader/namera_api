using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Owner;
using namera_API.Services.Owner;

namespace namera_API.Controllers.Owner;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/admin/reviews")]
public sealed class AdminReviewsController : ControllerBase
{
    private readonly IOwnerAccountService _ownerAccountService;

    public AdminReviewsController(IOwnerAccountService ownerAccountService)
    {
        _ownerAccountService = ownerAccountService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OwnerReviewResponseDto>>>> GetReviews([FromQuery] bool? visible)
    {
        return Ok(await _ownerAccountService.GetReviewsAsync(visible));
    }

    [HttpPatch("{reviewId:guid}/visibility")]
    public async Task<ActionResult<ApiResponse<OwnerReviewResponseDto>>> SetVisibility(Guid reviewId, UpdateReviewVisibilityRequestDto request)
    {
        var response = await _ownerAccountService.SetReviewVisibilityAsync(reviewId, request.IsVisible);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{reviewId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteReview(Guid reviewId)
    {
        var response = await _ownerAccountService.DeleteReviewAsync(reviewId);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
