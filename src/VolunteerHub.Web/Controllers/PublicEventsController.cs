using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Contracts.Requests;

namespace VolunteerHub.Web.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/events")]
public class PublicEventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public PublicEventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublishedEvents([FromQuery] SearchPublishedEventsRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.SearchPublishedEventsAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublishedEvent(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetPublishedEventAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { Error = result.Error });
    }
}
