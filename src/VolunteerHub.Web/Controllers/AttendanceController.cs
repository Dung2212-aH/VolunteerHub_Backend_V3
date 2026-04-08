using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Contracts.Constants;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Web.Infrastructure;

namespace VolunteerHub.Web.Controllers;

[ApiController]
[Route("api/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IVolunteerProfileRepository _profileRepository;

    public AttendanceController(IAttendanceService attendanceService, IVolunteerProfileRepository profileRepository)
    {
        _attendanceService = attendanceService;
        _profileRepository = profileRepository;
    }

    private async Task<Guid?> ResolveProfileIdAsync(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdWithDetailsAsync(User.GetUserId(), cancellationToken);
        return profile?.Id;
    }

    [Authorize(Roles = AppRoles.Volunteer)]
    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken cancellationToken)
    {
        var profileId = await ResolveProfileIdAsync(cancellationToken);
        if (profileId == null) return BadRequest(new { Error = "Volunteer profile not found." });
        var result = await _attendanceService.CheckInAsync(profileId.Value, request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [Authorize(Roles = AppRoles.Volunteer)]
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request, CancellationToken cancellationToken)
    {
        var profileId = await ResolveProfileIdAsync(cancellationToken);
        if (profileId == null) return BadRequest(new { Error = "Volunteer profile not found." });
        var result = await _attendanceService.CheckOutAsync(profileId.Value, request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [Authorize(Roles = AppRoles.Volunteer)]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAttendance(CancellationToken cancellationToken)
    {
        var profileId = await ResolveProfileIdAsync(cancellationToken);
        if (profileId == null) return BadRequest(new { Error = "Volunteer profile not found." });
        var result = await _attendanceService.GetMyAttendanceAsync(profileId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpGet("events/{eventId:guid}")]
    public async Task<IActionResult> GetEventAttendance(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetEventAttendanceAsync(User.GetUserId(), eventId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpPost("events/{eventId:guid}/volunteers/{volunteerProfileId:guid}/approve")]
    public async Task<IActionResult> ApproveAttendance(Guid eventId, Guid volunteerProfileId, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.ApproveAttendanceAsync(User.GetUserId(), eventId, volunteerProfileId, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpPost("events/{eventId:guid}/override")]
    public async Task<IActionResult> ManualOverride(Guid eventId, [FromBody] ManualOverrideRequest request, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.ManualOverrideAsync(User.GetUserId(), eventId, request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
