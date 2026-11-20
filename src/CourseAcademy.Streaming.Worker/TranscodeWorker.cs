namespace CourseAcademy.Streaming.Worker;

/// <summary>
/// Transcodes videos. Step S1 only checks the tools at start-up; leasing and processing jobs from the Streaming API
/// come in step S3 (docs/PLAN.md).
/// </summary>
public sealed class TranscodeWorker(FfmpegTools ffmpeg, IHostApplicationLifetime lifetime, ILogger<TranscodeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var info = await ffmpeg.InspectAsync(stoppingToken);
            logger.LogInformation("FFmpeg {FfmpegVersion}, ffprobe {FfprobeVersion}, NVENC {Nvenc}.",
                info.FfmpegVersion, info.FfprobeVersion, info.HasNvenc ? "built in" : "not available (CPU encoding)");
        }
        catch (InvalidOperationException exception)
        {
            // Without FFmpeg the worker can do nothing; stop with an error so the container restarts or the operator sees it.
            logger.LogCritical(exception, "The worker cannot run.");
            Environment.ExitCode = 1;
            lifetime.StopApplication();
            return;
        }

        logger.LogInformation("Worker ready. Job processing arrives in step S3.");
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }
}
