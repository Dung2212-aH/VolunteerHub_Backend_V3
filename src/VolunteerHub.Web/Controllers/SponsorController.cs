using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Contracts.Constants;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Web.Infrastructure;

namespace VolunteerHub.Web.Controllers;

[ApiController]
[Route("api/sponsor")]
public class SponsorController : ControllerBase
{
    private readonly ISponsorProfileService _sponsorProfileService;
    private readonly ISponsorManagementService _sponsorManagementService;

    public SponsorController(ISponsorProfileService sponsorProfileService, ISponsorManagementService sponsorManagementService)
    {
        _sponsorProfileService = sponsorProfileService;
        _sponsorManagementService = sponsorManagementService;
    }

    [Authorize(Roles = AppRoles.Sponsor)]
    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _sponsorProfileService.GetMyProfileAsync(User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Sponsor)]
    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateSponsorProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _sponsorProfileService.CreateProfileAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Sponsor)]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateSponsorProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _sponsorProfileService.UpdateProfileAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Sponsor)]
    [HttpPost("events/request")]
    public async Task<IActionResult> RequestSponsorEvent([FromBody] SponsorEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _sponsorProfileService.RequestSponsorEventAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Sponsor)]
    [HttpGet("events/my-sponsorships")]
    public async Task<IActionResult> GetMySponsorships(CancellationToken cancellationToken)
    {
        var result = await _sponsorProfileService.GetMyEventSponsorsAsync(User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpGet("organizer/events/{eventId:guid}/requests")]
    public async Task<IActionResult> GetEventSponsors(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _sponsorManagementService.GetEventSponsorsAsync(User.GetUserId(), eventId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpPost("organizer/requests/{eventSponsorId:guid}/review")]
    public async Task<IActionResult> ReviewEventSponsor(Guid eventSponsorId, [FromBody] ApproveRejectEventSponsorRequest request, CancellationToken cancellationToken)
    {
        var result = await _sponsorManagementService.ReviewEventSponsorAsync(User.GetUserId(), eventSponsorId, request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpPost("organizer/contributions")]
    public async Task<IActionResult> RecordContribution([FromBody] RecordContributionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sponsorManagementService.RecordContributionAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error });
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpPost("organizer/contributions/{contributionId:guid}/status")]
    public async Task<IActionResult> UpdateContributionStatus(Guid contributionId, [FromBody] UpdateContributionStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _sponsorManagementService.UpdateContributionStatusAsync(User.GetUserId(), contributionId, request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error });
    }
}
