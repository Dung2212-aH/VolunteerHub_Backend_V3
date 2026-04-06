using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Contracts.Constants;
using VolunteerHub.Contracts.Requests;

namespace VolunteerHub.Web.Controllers;

[Authorize(Roles = AppRoles.Organizer)]
[Route("api/organizer/profile")]
[ApiController]
public class OrganizerProfileController : ControllerBase
{
    private readonly IOrganizerProfileService _profileService;
    public OrganizerProfileController(IOrganizerProfileService profileService) { _profileService = profileService; }
    private Guid GetUserId() { var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier); return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty; }
    [HttpGet] public async Task<IActionResult> GetProfile(CancellationToken ct) { var result = await _profileService.GetProfileAsync(GetUserId(), ct); return result.IsSuccess ? Ok(result.Value) : NotFound(new { Error = result.Error }); }
    [HttpPost] public async Task<IActionResult> CreateProfile([FromBody] CreateOrganizerProfileRequest request, CancellationToken ct) { var result = await _profileService.CreateProfileAsync(GetUserId(), request, ct); return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error }); }
    [HttpPut] public async Task<IActionResult> UpdateProfile([FromBody] UpdateOrganizerProfileRequest request, CancellationToken ct) { var result = await _profileService.UpdateProfileAsync(GetUserId(), request, ct); return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error }); }
    [HttpPost("submit-verification")] public async Task<IActionResult> SubmitForVerification(CancellationToken ct) { var result = await _profileService.SubmitForVerificationAsync(GetUserId(), ct); return result.IsSuccess ? Ok() : BadRequest(new { Error = result.Error }); }
}
