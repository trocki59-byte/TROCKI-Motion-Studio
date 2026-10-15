using MotionStudio.Core;

// Kullanım:
// motionstudio --out video.mp4 [--aspect 16:9|9:16|1:1] [--seconds 5] [--fps 24|30|60]
//              [--motion none|zoom-in|zoom-out|pan-lr|pan-rl|pan-tb|pan-bt|cinematic]
//              [--speed very-slow|slow|normal|fast] [--fade on|off] [--ffmpeg yol] görsel1 görsel2 ...

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Kullanım: motionstudio --out video.mp4 [seçenekler] görsel1 [görsel2 ...]");
    Console.WriteLine("Seçenekler: --aspect --seconds --fps --motion --speed --fade --ffmpeg");
    return 0;
}

string? output = null, ffmpeg = null;
var images = new List<string>();
var settings = new VideoSettings();

try
{
    for (int i = 0; i < args.Length; i++)
    {
        string a = args[i];
        string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{a} için değer eksik.");

        switch (a)
        {
            case "--out": output = Next(); break;
            case "--ffmpeg": ffmpeg = Next(); break;
            case "--seconds": settings = settings with { SecondsPerImage = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture) }; break;
            case "--fps": settings = settings with { Fps = int.Parse(Next()) }; break;
            case "--aspect": settings = settings with { Aspect = Next() switch
            {
                "16:9" => AspectRatio.Landscape16x9,
                "9:16" => AspectRatio.Portrait9x16,
                "1:1" => AspectRatio.Square1x1,
                var v => throw new ArgumentException("Bilinmeyen oran: " + v)
            } }; break;
            case "--motion": settings = settings with { Motion = Next() switch
            {
                "none" => MotionType.None,
                "zoom-in" => MotionType.ZoomIn,
                "zoom-out" => MotionType.ZoomOut,
                "pan-lr" => MotionType.PanLeftToRight,
                "pan-rl" => MotionType.PanRightToLeft,
                "pan-tb" => MotionType.PanTopToBottom,
                "pan-bt" => MotionType.PanBottomToTop,
                "cinematic" => MotionType.Cinematic,
                var v => throw new ArgumentException("Bilinmeyen hareket: " + v)
            } }; break;
            case "--speed": settings = settings with { Speed = Next() switch
            {
                "very-slow" => SpeedLevel.VerySlow,
                "slow" => SpeedLevel.Slow,
                "normal" => SpeedLevel.Normal,
                "fast" => SpeedLevel.Fast,
                var v => throw new ArgumentException("Bilinmeyen hız: " + v)
            } }; break;
            case "--fade": settings = settings with { Transition = Next() == "off" ? TransitionType.None : TransitionType.Fade }; break;
            default:
                if (a.StartsWith("--")) throw new ArgumentException("Bilinmeyen seçenek: " + a);
                images.Add(a);
                break;
        }
    }

    if (output is null) throw new ArgumentException("--out belirtilmeli.");
    if (images.Count == 0) throw new ArgumentException("En az bir görsel verilmeli.");
    foreach (var img in images)
        if (!File.Exists(img)) throw new FileNotFoundException("Görsel bulunamadı: " + img);

    var renderer = new VideoRenderer(ffmpeg ?? VideoRenderer.LocateFfmpeg(AppContext.BaseDirectory));
    var progress = new Progress<double>(p => Console.Error.Write($"\rİlerleme: %{p * 100:0}"));
    await renderer.RenderAsync(images, output, settings, progress);
    Console.Error.WriteLine();
    Console.WriteLine("Tamamlandı: " + Path.GetFullPath(output));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Hata: " + ex.Message);
    return 1;
}
