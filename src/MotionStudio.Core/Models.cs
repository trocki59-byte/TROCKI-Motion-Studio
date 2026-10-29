namespace MotionStudio.Core;

/// <summary>Çıktı video oranı. Çözünürlükler <see cref="FfmpegCommandBuilder.Size"/> içinde.</summary>
public enum AspectRatio
{
    Landscape16x9,
    Portrait9x16,
    Square1x1
}

/// <summary>Sabit görüntü üzerindeki kamera hareketi.</summary>
public enum MotionType
{
    None,
    ZoomIn,
    ZoomOut,
    PanLeftToRight,
    PanRightToLeft,
    PanTopToBottom,
    PanBottomToTop,
    Cinematic
}

/// <summary>Hareketin ne kadarının klip içinde tamamlanacağını belirler (yavaş = daha az hareket).</summary>
public enum SpeedLevel
{
    VerySlow,
    Slow,
    Normal,
    Fast
}

public enum TransitionType
{
    None,
    Fade
}

/// <summary>Bir video üretimi için tüm ayarlar.</summary>
public sealed record VideoSettings
{
    public double SecondsPerImage { get; init; } = 5;
    public MotionType Motion { get; init; } = MotionType.ZoomIn;
    public SpeedLevel Speed { get; init; } = SpeedLevel.Normal;
    public TransitionType Transition { get; init; } = TransitionType.Fade;
    public int Fps { get; init; } = 30;
    public AspectRatio Aspect { get; init; } = AspectRatio.Landscape16x9;

    public void Validate()
    {
        if (SecondsPerImage < 0.5 || SecondsPerImage > 7200)
            throw new ArgumentOutOfRangeException(nameof(SecondsPerImage), "Görsel başına süre 0,5–7200 saniye olmalı.");
        if (Fps is not (24 or 30 or 60))
            throw new ArgumentOutOfRangeException(nameof(Fps), "FPS 24, 30 veya 60 olmalı.");
    }
}
