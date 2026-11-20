using System.Reflection;

namespace CourseAcademy.Streaming.Api.Common;

public static class ServiceInfo
{
    public const string Name = "course-academy-streaming";

    /// <summary>The build's informational version without the source hash, e.g. "1.0.0".</summary>
    public static readonly string Version =
        (typeof(ServiceInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];
}
