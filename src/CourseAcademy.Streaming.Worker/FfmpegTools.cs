using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Streaming.Worker;

/// <summary>What the installed FFmpeg can do, found at start-up.</summary>
public sealed record FfmpegInfo(string FfmpegVersion, string FfprobeVersion, bool HasNvenc);

/// <summary>Runs the FFmpeg programs.</summary>
public sealed partial class FfmpegTools(IOptions<WorkerSettings> settings)
{
    /// <summary>NVIDIA's H.264 encoder. Being listed only means FFmpeg was built with it; S3 checks it with a test encode.</summary>
    public const string NvencEncoder = "h264_nvenc";

    /// <summary>Checks that ffmpeg and ffprobe run, and which encoders this FFmpeg build has.</summary>
    public async Task<FfmpegInfo> InspectAsync(CancellationToken ct)
    {
        var ffmpeg = await RunAsync(settings.Value.FfmpegPath, ["-hide_banner", "-version"], ct);
        var ffprobe = await RunAsync(settings.Value.FfprobePath, ["-hide_banner", "-version"], ct);
        var encoders = await RunAsync(settings.Value.FfmpegPath, ["-hide_banner", "-encoders"], ct);
        return new FfmpegInfo(ParseVersion(ffmpeg), ParseVersion(ffprobe), HasEncoder(encoders, NvencEncoder));
    }

    /// <summary>"ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright …" → "9.0.2-full_build-www.gyan.dev".</summary>
    public static string ParseVersion(string output)
    {
        var match = VersionLine().Match(output);
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    /// <summary>True when the output of "ffmpeg -encoders" lists the encoder (lines look like " V....D h264_nvenc   NVIDIA …").</summary>
    public static bool HasEncoder(string encodersOutput, string name) =>
        encodersOutput.Split('\n').Any(line =>
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && parts[1] == name;
        });

    /// <summary>Runs a program and returns its standard output. Throws when it cannot start or exits with an error.</summary>
    private static async Task<string> RunAsync(string program, IReadOnlyList<string> arguments, CancellationToken ct)
    {
        var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException($"\"{program}\" could not be started. Install FFmpeg or set Worker:FfmpegPath / Worker:FfprobePath.", exception);
        }

        var output = process.StandardOutput.ReadToEndAsync(ct);
        var error = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"\"{program}\" exited with code {process.ExitCode}: {(await error).Trim()}");
        }
        return await output;
    }

    [GeneratedRegex(@"^\S+ version (\S+)", RegexOptions.Multiline)]
    private static partial Regex VersionLine();
}
