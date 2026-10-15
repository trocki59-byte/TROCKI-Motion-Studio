using System.Diagnostics;

namespace MotionStudio.Core;

/// <summary>Görselleri klipe, klipleri tek MP4'e dönüştürür. FFmpeg'i ayrı bir süreç olarak çalıştırır.</summary>
public sealed class VideoRenderer
{
    public string FfmpegPath { get; }

    public VideoRenderer(string ffmpegPath = "ffmpeg")
    {
        FfmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Görselleri sırayla birleştirip <paramref name="outputPath"/> dosyasını üretir.
    /// Progress değeri 0–1 arasında, klip bazında raporlanır.
    /// </summary>
    public async Task RenderAsync(
        IReadOnlyList<string> images,
        string outputPath,
        VideoSettings settings,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        settings.Validate();
        if (images.Count == 0)
            throw new ArgumentException("En az bir görsel seçilmeli.", nameof(images));

        string? outDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

        string work = Path.Combine(Path.GetTempPath(), "motionstudio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);

        try
        {
            var clips = new List<string>(images.Count);
            for (int i = 0; i < images.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                string clip = Path.Combine(work, $"clip_{i:D3}.mp4");
                await RunFfmpegAsync(FfmpegCommandBuilder.BuildClipArgs(images[i], clip, settings), ct);
                clips.Add(clip);
                progress?.Report((i + 1) / (double)(images.Count + 1));
            }

            string list = Path.Combine(work, "clips.txt");
            await File.WriteAllTextAsync(list, FfmpegCommandBuilder.ConcatListContent(clips), ct);
            await RunFfmpegAsync(FfmpegCommandBuilder.BuildConcatArgs(list, outputPath), ct);
            progress?.Report(1);
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { /* geçici dosyalar sonra temizlenebilir */ }
        }
    }

    private async Task RunFfmpegAsync(string[] args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(FfmpegPath)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("FFmpeg başlatılamadı: " + FfmpegPath);

        Task<string> stderr = process.StandardError.ReadToEndAsync(ct);
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(ct);

        using var cancelReg = ct.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        await process.WaitForExitAsync(ct);
        string err = await stderr;
        await stdout;

        if (process.ExitCode != 0)
            throw new InvalidOperationException("FFmpeg hatası (kod " + process.ExitCode + "): " + err.Trim());
    }

    /// <summary>FFmpeg'i uygulama klasöründeki <c>ffmpeg</c> alt klasöründe ya da PATH üzerinde arar.</summary>
    public static string LocateFfmpeg(string appBaseDir)
    {
        string[] candidates =
        [
            Path.Combine(appBaseDir, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(appBaseDir, "ffmpeg.exe"),
            Path.Combine(appBaseDir, "ffmpeg", "ffmpeg"),
            Path.Combine(appBaseDir, "ffmpeg")
        ];
        foreach (var c in candidates)
            if (File.Exists(c)) return c;
        return "ffmpeg";
    }
}
