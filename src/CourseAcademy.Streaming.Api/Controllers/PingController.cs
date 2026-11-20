using CourseAcademy.Streaming.Api.Common;
using CourseAcademy.Streaming.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Streaming.Api.Controllers;

/// <summary>Internal API for the Main App. Every endpoint needs the X-Api-Key header.</summary>
[ApiController]
[Route("internal/v1")]
[Produces("application/json")]
public sealed class PingController(TimeProvider clock) : ControllerBase
{
    /// <summary>Lets the Main App check that the service is running and accepts its API key.</summary>
    [HttpGet("ping")]
    [ProducesResponseType<PingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public PingDto Ping() => new(ServiceInfo.Name, ServiceInfo.Version, clock.GetUtcNow().UtcDateTime);
}
