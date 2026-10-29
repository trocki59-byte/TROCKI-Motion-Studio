using System.Text.Json;

namespace MotionStudio.Core;

public enum StudioMediaKind { Image, Video }

public sealed record StudioClip
{
    public string SourcePath { get; init; } = "";
    public StudioMediaKind Kind { get; init; } = StudioMediaKind.Image;
    public double DurationSeconds { get; init; } = 5;
    public double TrimStartSeconds { get; init; } = 0;
    public MotionType Motion { get; init; } = MotionType.Cinematic;
}

public sealed record StudioProject
{
    public List<StudioClip> Clips { get; init; } = new();
    public string? MusicPath { get; init; }
    public string? NarrationPath { get; init; }
    public double MusicVolume { get; init; } = 0.20;
    public double NarrationVolume { get; init; } = 1.0;
    public int Fps { get; init; } = 30;
    public AspectRatio Aspect { get; init; } = AspectRatio.Landscape16x9;
    public bool Fade { get; init; } = true;

    public double TotalSeconds => Clips.Sum(c => c.DurationSeconds);

    public void Validate()
    {
        if (Clips.Count == 0) throw new InvalidOperationException("Zaman çizelgesine en az bir görsel veya video ekleyin.");
        if (Fps is not (24 or 30 or 60)) throw new ArgumentException("Desteklenen FPS: 24, 30, 60.");
        foreach (var clip in Clips)
        {
            if (clip.DurationSeconds < 0.5 || clip.DurationSeconds > 7200)
                throw new ArgumentException("Klip süresi 0,5 ile 7200 saniye arasında olmalı.");
            if (clip.TrimStartSeconds < 0) throw new ArgumentException("Kırpma başlangıcı negatif olamaz.");
            if (!File.Exists(clip.SourcePath)) throw new FileNotFoundException("Medya bulunamadı", clip.SourcePath);
        }
        foreach (var audio in new[] { MusicPath, NarrationPath })
            if (!string.IsNullOrWhiteSpace(audio) && !File.Exists(audio))
                throw new FileNotFoundException("Ses dosyası bulunamadı", audio);
        if (MusicVolume is < 0 or > 2 || NarrationVolume is < 0 or > 2)
            throw new ArgumentOutOfRangeException("Ses düzeyi 0 ile 2 arasında olmalı.");
    }

    public static async Task<StudioProject> LoadAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<StudioProject>(json)
            ?? throw new InvalidDataException("Proje dosyası okunamadı.");
    }

    public async Task SaveAsync(string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(this, options));
    }
}
