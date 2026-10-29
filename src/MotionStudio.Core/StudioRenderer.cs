using System.Diagnostics;
using System.Globalization;

namespace MotionStudio.Core;

/// <summary>Görsel/video zaman çizelgesini birleştirir, müzik ve seslendirmeyi MP4'e miksler.</summary>
public sealed class StudioRenderer
{
    private readonly string _ffmpeg;
    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public StudioRenderer(string ffmpegPath) => _ffmpeg = ffmpegPath;

    public async Task RenderAsync(StudioProject project, string outputPath,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        project.Validate();
        string fullOutput = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutput)!);
        string working = Path.Combine(Path.GetTempPath(), "trocki-motion-pro-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(working);

        try
        {
            var segments = new List<string>();
            for (int i = 0; i < project.Clips.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var clip = project.Clips[i];
                string segmentPath = Path.Combine(working, "segment_" + i.ToString("D4") + ".mp4");
                string[] arguments = clip.Kind == StudioMediaKind.Image
                    ? ImageArgs(clip, project, segmentPath)
                    : VideoArgs(clip, project, segmentPath);
                await RunAsync(arguments, cancellationToken);
                segments.Add(segmentPath);
                progress?.Report((i + 1) * 0.85 / project.Clips.Count);
            }

            string listPath = Path.Combine(working, "segments.txt");
            await File.WriteAllTextAsync(listPath, FfmpegCommandBuilder.ConcatListContent(segments), cancellationToken);
            string silent = Path.Combine(working, "silent.mp4");
            await RunAsync(FfmpegCommandBuilder.BuildConcatArgs(listPath, silent), cancellationToken);
            progress?.Report(0.90);

            if (string.IsNullOrWhiteSpace(project.MusicPath) && string.IsNullOrWhiteSpace(project.NarrationPath))
            {
                File.Move(silent, fullOutput, overwrite: true);
            }
            else
            {
                await RunAsync(AudioArgs(project, silent, fullOutput), cancellationToken);
            }
            progress?.Report(1);
        }
        finally
        {
            try { Directory.Delete(working, true); } catch { /* işlem sürerken dosyalar kilitli olabilir */ }
        }
    }

    private static string[] ImageArgs(StudioClip clip, StudioProject project, string path)
    {
        // Temel resim motoru ile aynı filtreleri kullan; tek klip için süre sınırı daha uzundur.
        var settings = new VideoSettings
        {
            SecondsPerImage = clip.DurationSeconds,
            Motion = clip.Motion,
            Fps = project.Fps,
            Aspect = project.Aspect,
            Transition = project.Fade ? TransitionType.Fade : TransitionType.None
        };
        return FfmpegCommandBuilder.BuildClipArgs(clip.SourcePath, path, settings);
    }

    private static string[] VideoArgs(StudioClip clip, StudioProject project, string path)
    {
        var (width, height) = FfmpegCommandBuilder.Size(project.Aspect);
        string filter = "scale=" + width + ":" + height
            + ":force_original_aspect_ratio=decrease,"
            + "pad=" + width + ":" + height + ":(ow-iw)/2:(oh-ih)/2,"
            + "setsar=1,fps=" + project.Fps + ",format=yuv420p";
        if (project.Fade)
        {
            double fadeSeconds = Math.Min(0.5, clip.DurationSeconds / 3);
            filter += ",fade=t=in:st=0:d=" + N(fadeSeconds)
                + ",fade=t=out:st=" + N(clip.DurationSeconds - fadeSeconds) + ":d=" + N(fadeSeconds);
        }
        return new[]
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-ss", N(clip.TrimStartSeconds), "-i", Path.GetFullPath(clip.SourcePath),
            "-t", N(clip.DurationSeconds), "-vf", filter,
            "-r", project.Fps.ToString(CultureInfo.InvariantCulture),
            "-c:v", "libx264", "-preset", "medium", "-crf", "20", "-pix_fmt", "yuv420p",
            "-an", "-movflags", "+faststart", Path.GetFullPath(path)
        };
    }

    private static string[] AudioArgs(StudioProject project, string videoPath, string outputPath)
    {
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y", "-i", videoPath };
        var labels = new List<string>();
        var parts = new List<string>();
        int input = 1;
        string duration = N(project.TotalSeconds);

        if (!string.IsNullOrWhiteSpace(project.MusicPath))
        {
            args.AddRange(new[] { "-stream_loop", "-1", "-i", Path.GetFullPath(project.MusicPath) });
            parts.Add("[" + input++ + ":a]volume=" + N(project.MusicVolume)
                + ",aresample=48000,aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo"
                + ",atrim=duration=" + duration + ",asetpts=PTS-STARTPTS[music]");
            labels.Add("[music]");
        }
        if (!string.IsNullOrWhiteSpace(project.NarrationPath))
        {
            args.AddRange(new[] { "-i", Path.GetFullPath(project.NarrationPath) });
            parts.Add("[" + input++ + ":a]volume=" + N(project.NarrationVolume)
                + ",aresample=48000,aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo"
                + ",apad,atrim=duration=" + duration + ",asetpts=PTS-STARTPTS[voice]");
            labels.Add("[voice]");
        }
        if (labels.Count == 2)
            parts.Add("[music][voice]amix=inputs=2:duration=longest:normalize=0[a]");
        else
            parts.Add(labels[0] + "anull[a]");

        args.AddRange(new[]
        {
            "-filter_complex", string.Join(";", parts), "-map", "0:v:0", "-map", "[a]",
            "-c:v", "copy", "-c:a", "aac", "-b:a", "192k",
            "-t", duration, "-movflags", "+faststart", Path.GetFullPath(outputPath)
        });
        return args.ToArray();
    }

    private async Task RunAsync(string[] args, CancellationToken ct)
    {
        var start = new ProcessStartInfo(_ffmpeg)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        foreach (string arg in args) start.ArgumentList.Add(arg);

        using var process = Process.Start(start) ??
            throw new InvalidOperationException("FFmpeg çalıştırılamadı: " + _ffmpeg);
        using var registration = ct.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });
        Task<string> errorTask = process.StandardError.ReadToEndAsync(ct);
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        string error = await errorTask;
        await outputTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException("FFmpeg çıkış kodu " + process.ExitCode + ": " + error.Trim());
    }
}
