using System.Diagnostics;
using System.Text;

namespace MotionStudio.App;

/// <summary>Windows yerel SAPI sesleriyle internet gerektirmeden WAV seslendirme üretir.</summary>
public static class SpeechService
{
    public static async Task<IReadOnlyList<string>> GetVoicesAsync(CancellationToken ct = default)
    {
        const string script = @"
Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    foreach ($v in $s.GetInstalledVoices()) {
        if ($v.Enabled) { Write-Output $v.VoiceInfo.Name }
    }
} finally { $s.Dispose() }";
        string output = await RunScriptAsync(script, ct);
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static async Task SynthesizeAsync(string text, string destination,
        string? voiceName, int rate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Seslendirme metni boş olamaz.");
        if (rate is < -10 or > 10)
            throw new ArgumentOutOfRangeException(nameof(rate));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        string input = Path.Combine(Path.GetTempPath(), "motion-tts-" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            await File.WriteAllTextAsync(input, text, new UTF8Encoding(false), ct);
            string selectedVoice = string.IsNullOrWhiteSpace(voiceName)
                ? "" : "$s.SelectVoice('" + Escape(voiceName) + "')";

            string script = @"
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    " + selectedVoice + @"
    $s.Rate = " + rate + @"
    $s.SetOutputToWaveFile('" + Escape(Path.GetFullPath(destination)) + @"')
    $content = [System.IO.File]::ReadAllText('" + Escape(input) + @"', [System.Text.Encoding]::UTF8)
    $s.Speak($content)
} finally { $s.Dispose() }";

            await RunScriptAsync(script, ct);
            if (!File.Exists(destination) || new FileInfo(destination).Length == 0)
                throw new IOException("Windows seslendirme WAV dosyasını oluşturmadı.");
        }
        finally
        {
            try { File.Delete(input); } catch { }
        }
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static async Task<string> RunScriptAsync(string script, CancellationToken ct)
    {
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", encoded })
            start.ArgumentList.Add(arg);

        using var proc = Process.Start(start) ??
            throw new InvalidOperationException("Windows PowerShell başlatılamadı.");
        using var registration = ct.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
        });
        Task<string> stdout = proc.StandardOutput.ReadToEndAsync(ct);
        Task<string> stderr = proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        string output = await stdout;
        string error = await stderr;
        if (proc.ExitCode != 0)
            throw new InvalidOperationException("Windows seslendirme hatası: " + error.Trim());
        return output.Trim();
    }
}
