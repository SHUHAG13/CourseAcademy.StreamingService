namespace CourseAcademy.Streaming.Api.Dtos;

/// <summary>Answer of GET /internal/v1/ping.</summary>
public sealed record PingDto(string Service, string Version, DateTime ServerTime);

/// <summary>Answer of GET /health. <see cref="Checks"/> maps each check (e.g. "database") to Healthy, Degraded or Unhealthy.</summary>
public sealed record HealthDto(string Status, IReadOnlyDictionary<string, string> Checks);
