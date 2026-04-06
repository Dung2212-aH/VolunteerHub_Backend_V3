using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Contracts.Constants;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Web.Infrastructure;

namespace VolunteerHub.Web.Controllers;

[Authorize(Roles = AppRoles.Organizer)]
[Route("api/organizer/applications")]
[ApiController]
public class OrganizerApplicationReviewController : ControllerBase
{
    private readonly IApplicationReviewService _reviewService;
    public OrganizerApplicationReviewController(IApplicationReviewService reviewService) { _reviewService = reviewService; }
    [HttpGet("events/{eventId:guid}")] public async Task<IActionResult> GetEventApplications(Guid eventId, CancellationToken ct) { var result = await _reviewService.GetEventApplicationsAsync(User.GetUserId(), eventId, ct); return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Error = result.Error }); }
    [HttpGet("{applicationId:guid}")] public async Task<IActionResult> GetApplication(Guid applicationId, CancellationToken ct) { var result = await _reviewService.GetApplicationDetailsAsync(User.GetUserId(), applicationId, ct); return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Error = result.Error }); }
    [HttpPost("{applicationId:guid}/approve")] public async Task<IActionResult> Approve(Guid applicationId, CancellationToken ct) { var result = await _reviewService.ApproveAsync(User.GetUserId(), applicationId, ct); return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error }); }
    [HttpPost("{applicationId:guid}/reject")] public async Task<IActionResult> Reject(Guid applicationId, [FromBody] ReviewApplicationRequest request, CancellationToken ct) { var result = await _reviewService.RejectAsync(User.GetUserId(), applicationId, request, ct); return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error }); }
    [HttpPost("{applicationId:guid}/waitlist")] public async Task<IActionResult> Waitlist(Guid applicationId, CancellationToken ct) { var result = await _reviewService.WaitlistAsync(User.GetUserId(), applicationId, ct); return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error }); }
}
