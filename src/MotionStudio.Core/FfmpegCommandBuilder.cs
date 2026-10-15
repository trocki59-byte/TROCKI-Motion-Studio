using System.Globalization;

namespace MotionStudio.Core;

/// <summary>
/// Görselden klip ve klipten final MP4 üretmek için FFmpeg argümanlarını hazırlar.
/// Süreç başlatmaz; yalnızca argüman dizilerini döndürür (test edilebilir).
/// </summary>
public static class FfmpegCommandBuilder
{
    private const double ZoomAmount = 0.25;    // Zoom In/Out için 1.0 → 1.25
    private const double PanZoom = 1.2;        // Pan sırasındaki sabit yakınlaştırma
    private const double CinematicZoom = 0.2;  // Sinematik efektte zoom miktarı
    private const double FadeSeconds = 0.5;

    public static (int Width, int Height) Size(AspectRatio aspect) => aspect switch
    {
        AspectRatio.Landscape16x9 => (1920, 1080),
        AspectRatio.Portrait9x16 => (1080, 1920),
        AspectRatio.Square1x1 => (1080, 1080),
        _ => throw new ArgumentOutOfRangeException(nameof(aspect))
    };

    public static double SpeedFactor(SpeedLevel speed) => speed switch
    {
        SpeedLevel.VerySlow => 0.5,
        SpeedLevel.Slow => 0.75,
        SpeedLevel.Normal => 1.0,
        SpeedLevel.Fast => 1.5,
        _ => 1.0
    };

    /// <summary>Tek bir görselden, ayarlara uygun MP4 klip üretir.</summary>
    public static string[] BuildClipArgs(string imagePath, string outputPath, VideoSettings settings)
    {
        settings.Validate();
        var (w, h) = Size(settings.Aspect);
        int frames = Math.Max(2, (int)Math.Round(settings.SecondsPerImage * settings.Fps));

        string filter = BuildClipFilter(settings, w, h, frames);

        return
        [
            "-hide_banner", "-loglevel", "error", "-y",
            "-loop", "1", "-i", Path.GetFullPath(imagePath),
            "-frames:v", frames.ToString(CultureInfo.InvariantCulture),
            "-vf", filter,
            "-r", settings.Fps.ToString(CultureInfo.InvariantCulture),
            "-c:v", "libx264", "-preset", "medium", "-crf", "20", "-pix_fmt", "yuv420p",
            "-movflags", "+faststart",
            "-an",
            Path.GetFullPath(outputPath)
        ];
    }

    /// <summary>Aynı kodlama ayarlarıyla üretilmiş klipleri kayıpsız birleştirir.</summary>
    public static string[] BuildConcatArgs(string listFile, string outputPath) =>
    [
        "-hide_banner", "-loglevel", "error", "-y",
        "-f", "concat", "-safe", "0", "-i", Path.GetFullPath(listFile),
        "-c", "copy",
        "-movflags", "+faststart",
        Path.GetFullPath(outputPath)
    ];

    /// <summary>concat demuxer için liste dosyası içeriği.</summary>
    public static string ConcatListContent(IEnumerable<string> clipPaths) =>
        string.Join("\n", clipPaths.Select(p => "file '" + Path.GetFullPath(p).Replace("'", "'\\''") + "'")) + "\n";

    /// <summary>
    /// Zoompan filtresi. P = hareketin ilerleme oranı (0→1), hıza göre hesaplanır ve 1'de sabitlenir.
    /// Girdi görsel önce 2x boyuta getirilir; böylece zoom/pan sırasında titreme azalır.
    /// </summary>
    public static string BuildClipFilter(VideoSettings settings, int width, int height, int frames)
    {
        double k = SpeedFactor(settings.Speed);
        string kStr = k.ToString("0.###", CultureInfo.InvariantCulture);
        string progress = $"min(1,on*{kStr}/{frames - 1})";

        string centerX = "iw/2-(iw/zoom/2)";
        string centerY = "ih/2-(ih/zoom/2)";
        string z, x, y;

        switch (settings.Motion)
        {
            case MotionType.None:
                z = "1"; x = "0"; y = "0";
                break;
            case MotionType.ZoomIn:
                z = $"1+{Num(ZoomAmount)}*{progress}"; x = centerX; y = centerY;
                break;
            case MotionType.ZoomOut:
                z = $"{Num(1 + ZoomAmount)}-{Num(ZoomAmount)}*{progress}"; x = centerX; y = centerY;
                break;
            case MotionType.PanLeftToRight:
                z = Num(PanZoom); x = $"(iw-iw/zoom)*{progress}"; y = centerY;
                break;
            case MotionType.PanRightToLeft:
                z = Num(PanZoom); x = $"(iw-iw/zoom)*(1-{progress})"; y = centerY;
                break;
            case MotionType.PanTopToBottom:
                z = Num(PanZoom); x = centerX; y = $"(ih-ih/zoom)*{progress}";
                break;
            case MotionType.PanBottomToTop:
                z = Num(PanZoom); x = centerX; y = $"(ih-ih/zoom)*(1-{progress})";
                break;
            case MotionType.Cinematic:
                z = $"1+{Num(CinematicZoom)}*{progress}"; x = $"(iw-iw/zoom)*{progress}"; y = centerY;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(settings));
        }

        var parts = new List<string>
        {
            $"scale={width * 2}:{height * 2}:force_original_aspect_ratio=increase",
            $"crop={width * 2}:{height * 2}",
            $"zoompan=z='{z}':x='{x}':y='{y}':d={frames}:s={width}x{height}:fps={settings.Fps.ToString(CultureInfo.InvariantCulture)}",
            "setsar=1",
            "format=yuv420p"
        };

        if (settings.Transition == TransitionType.Fade)
        {
            double fade = Math.Min(FadeSeconds, settings.SecondsPerImage / 3);
            string fadeStr = Num(fade);
            string outStart = Num(settings.SecondsPerImage - fade);
            parts.Add($"fade=t=in:st=0:d={fadeStr}");
            parts.Add($"fade=t=out:st={outStart}:d={fadeStr}");
        }

        return string.Join(",", parts);
    }

    private static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
