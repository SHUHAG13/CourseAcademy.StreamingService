using CourseAcademy.Streaming.Worker;

namespace CourseAcademy.Streaming.Tests.Worker;

public class FfmpegToolsTests
{
    [Fact]
    public void The_version_is_read_from_the_first_line()
    {
        Assert.Equal("9.0.2-full_build-www.gyan.dev",
            FfmpegTools.ParseVersion("ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers\nbuilt with gcc"));
        Assert.Equal("7.1", FfmpegTools.ParseVersion("ffprobe version 7.1 Copyright (c) 2007-2024"));
        Assert.Equal("unknown", FfmpegTools.ParseVersion("something else"));
    }

    [Fact]
    public void An_encoder_counts_only_when_it_is_listed_by_name()
    {
        const string output = """
            Encoders:
             V..... = Video
             ------
             V....D libx264              libx264 H.264 / AVC / MPEG-4 AVC (codec h264)
             V....D h264_nvenc           NVIDIA NVENC H.264 encoder (codec h264)
            """;

        Assert.True(FfmpegTools.HasEncoder(output, "h264_nvenc"));
        Assert.True(FfmpegTools.HasEncoder(output, "libx264"));
        Assert.False(FfmpegTools.HasEncoder(output, "hevc_nvenc"));
        Assert.False(FfmpegTools.HasEncoder(output, "NVIDIA"));
    }
}
