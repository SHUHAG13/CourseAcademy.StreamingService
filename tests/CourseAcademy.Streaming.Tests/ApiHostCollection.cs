namespace CourseAcademy.Streaming.Tests;

/// <summary>
/// Tests that start the API in memory. Program.cs sets Serilog's static logger, so two hosts must never start at the
/// same time; this collection runs its tests one after another.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiHostCollection
{
    public const string Name = "API host";
}
