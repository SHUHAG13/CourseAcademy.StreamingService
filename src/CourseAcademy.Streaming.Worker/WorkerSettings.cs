using System.ComponentModel.DataAnnotations;

namespace CourseAcademy.Streaming.Worker;

/// <summary>Section "Worker" in configuration.</summary>
public sealed class WorkerSettings
{
    public const string SectionName = "Worker";

    /// <summary>The ffmpeg program: a name found on PATH, or a full path.</summary>
    [Required(AllowEmptyStrings = false)]
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>The ffprobe program: a name found on PATH, or a full path.</summary>
    [Required(AllowEmptyStrings = false)]
    public string FfprobePath { get; set; } = "ffprobe";

    /// <summary>Folder for downloaded originals and encoded pieces. Needs room for the largest original plus its output.</summary>
    [Required(AllowEmptyStrings = false)]
    public string WorkDirectory { get; set; } = "work";
}
