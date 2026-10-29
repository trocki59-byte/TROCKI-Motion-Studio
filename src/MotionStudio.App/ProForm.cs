using System.Diagnostics;
using MotionStudio.Core;

namespace MotionStudio.App;

/// <summary>Windows masaüstü: zaman çizelgesi, yerel seslendirme ve MP4 dışa aktarma.</summary>
public sealed class ProForm : Form
{
    private sealed record Option<T>(string Caption, T Value)
    {
        public override string ToString() => Caption;
    }

    private static readonly Color BackgroundColor = Color.FromArgb(22, 26, 33);
    private static readonly Color PanelColor = Color.FromArgb(34, 41, 52);
    private static readonly Color TextColor = Color.FromArgb(236, 240, 247);
    private static readonly Color Accent = Color.FromArgb(52, 125, 220);

    private StudioProject _project = new();
    private string? _projectFile;
    private CancellationTokenSource? _operation;
    private bool _updating;

    private readonly ListBox _clips = new();
    private readonly PictureBox _preview = new();
    private readonly Label _previewInfo = new();
    private readonly NumericUpDown _duration = new();
    private readonly NumericUpDown _trimStart = new();
    private readonly ComboBox _clipMotion = new();
    private readonly Button _openClip = new();

    private readonly TextBox _script = new();
    private readonly ComboBox _voices = new();
    private readonly NumericUpDown _voiceRate = new();
    private readonly Button _generateSpeech = new();
    private readonly Button _refreshVoices = new();
    private readonly Label _ttsStatus = new();

    private readonly TextBox _musicPath = new();
    private readonly TextBox _narrationPath = new();
    private readonly NumericUpDown _musicLevel = new();
    private readonly NumericUpDown _narrationLevel = new();
    private readonly ComboBox _aspect = new();
    private readonly ComboBox _fps = new();
    private readonly CheckBox _fade = new();
    private readonly Button _render = new();
    private readonly Button _cancel = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _status = new();

    public ProForm()
    {
        Text = "TROÇKİ Motion Studio PRO | Video Düzenleme & Seslendirme";
        MinimumSize = new Size(1080, 740);
        Size = new Size(1290, 845);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BackgroundColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10);

        var title = new Label
        {
            Text = "TROÇKİ MOTION STUDIO PRO",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(14, 8, 0, 0)
        };
        var subtitle = new Label
        {
            Text = "Windows masaüstü video stüdyosu · Fotoğraf + video · Seslendirme · Müzik · MP4",
            ForeColor = Color.LightSteelBlue,
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(17, 0, 0, 0)
        };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildEditor());
        tabs.TabPages.Add(BuildNarration());
        tabs.TabPages.Add(BuildOutput());
        Controls.Add(tabs);
        Controls.Add(subtitle);
        Controls.Add(title);

        RefreshProject();
        FormClosing += (_, _) => _operation?.Cancel();
    }

    private static Label Caption(string text, int x, int y, int width = 300) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(width, 25), ForeColor = TextColor
    };

    private static Button Action(string text, int x, int y, int width, EventHandler callback)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 37),
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += callback;
        return button;
    }

    private static NumericUpDown Number(decimal minimum, decimal maximum, decimal value,
        int x, int y, int width = 120, int decimals = 0)
    {
        return new NumericUpDown
        {
            Minimum = minimum, Maximum = maximum, Value = value, DecimalPlaces = decimals,
            Increment = decimals == 0 ? 1 : .5m, Location = new Point(x, y),
            Size = new Size(width, 30)
        };
    }

    private TabPage BuildEditor()
    {
        var tab = new TabPage("1. Video Düzenleme") { BackColor = BackgroundColor, ForeColor = TextColor };
        var left = new Panel { Dock = DockStyle.Left, Width = 500, BackColor = PanelColor };
        var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        tab.Controls.Add(right);
        tab.Controls.Add(left);

        left.Controls.Add(Caption("ZAMAN ÇİZELGESİ — SAHNELER", 16, 14, 420));
        left.Controls.Add(Action("📷 Fotoğraf Ekle", 14, 47, 225, (_, _) => AddMedia(false)));
        left.Controls.Add(Action("🎞 Video Ekle", 253, 47, 225, (_, _) => AddMedia(true)));

        _clips.Location = new Point(14, 94);
        _clips.Size = new Size(464, 325);
        _clips.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _clips.BackColor = BackgroundColor;
        _clips.ForeColor = TextColor;
        _clips.BorderStyle = BorderStyle.FixedSingle;
        _clips.SelectedIndexChanged += (_, _) => LoadSelected();
        left.Controls.Add(_clips);

        int buttonY = 430;
        left.Controls.Add(Action("↑ Yukarı", 14, buttonY, 143, (_, _) => MoveSelected(-1)));
        left.Controls.Add(Action("↓ Aşağı", 169, buttonY, 143, (_, _) => MoveSelected(1)));
        left.Controls.Add(Action("Sil", 324, buttonY, 154, (_, _) => RemoveSelected()));

        left.Controls.Add(Caption("Sahne süresi (saniye)", 16, 483));
        _duration.Minimum = .5m;
        _duration.Maximum = 7200;
        _duration.DecimalPlaces = 1;
        _duration.Increment = .5m;
        _duration.Location = new Point(290, 483);
        _duration.Size = new Size(188, 29);
        left.Controls.Add(_duration);

        left.Controls.Add(Caption("Videoda başlangıç (saniye)", 16, 524));
        _trimStart.Minimum = 0;
        _trimStart.Maximum = 7200;
        _trimStart.DecimalPlaces = 1;
        _trimStart.Increment = .5m;
        _trimStart.Location = new Point(290, 524);
        _trimStart.Size = new Size(188, 29);
        left.Controls.Add(_trimStart);

        left.Controls.Add(Caption("Görsel kamera hareketi", 16, 565));
        _clipMotion.Location = new Point(238, 562);
        _clipMotion.Size = new Size(240, 30);
        _clipMotion.DropDownStyle = ComboBoxStyle.DropDownList;
        _clipMotion.Items.AddRange(new object[]
        {
            new Option<MotionType>("Sabit", MotionType.None),
            new Option<MotionType>("Yakınlaş", MotionType.ZoomIn),
            new Option<MotionType>("Uzaklaş", MotionType.ZoomOut),
            new Option<MotionType>("Pan: Sol → Sağ", MotionType.PanLeftToRight),
            new Option<MotionType>("Pan: Sağ → Sol", MotionType.PanRightToLeft),
            new Option<MotionType>("Pan: Yukarı → Aşağı", MotionType.PanTopToBottom),
            new Option<MotionType>("Pan: Aşağı → Yukarı", MotionType.PanBottomToTop),
            new Option<MotionType>("Sinematik", MotionType.Cinematic)
        });
        _clipMotion.SelectedIndex = 7;
        left.Controls.Add(_clipMotion);
        left.Controls.Add(Action("Sahne Ayarlarını Uygula", 14, 614, 464, (_, _) => ApplySelected()));

        right.Controls.Add(Caption("SAHNE ÖNİZLEME", 18, 14, 600));
        _preview.Location = new Point(18, 50);
        _preview.Size = new Size(680, 390);
        _preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.BackColor = Color.Black;
        right.Controls.Add(_preview);
        _previewInfo.Location = new Point(18, 454);
        _previewInfo.Size = new Size(690, 100);
        _previewInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _previewInfo.ForeColor = TextColor;
        right.Controls.Add(_previewInfo);

        _openClip.Text = "🎬 Seçili videoyu oynat";
        _openClip.Location = new Point(18, 565);
        _openClip.Size = new Size(250, 38);
        _openClip.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _openClip.Click += (_, _) => OpenSelectedVideo();
        right.Controls.Add(_openClip);

        right.Controls.Add(new Label
        {
            Text = "İpucu: Sahneleri ↑ ↓ ile sırala; videoyu başlangıç ve süre değerleriyle kes. "
                 + "Fotoğrafa hareket ver; oluşturduğun filmi Çıktı sekmesinden dışa aktar.",
            Location = new Point(18, 616), Size = new Size(690, 65),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = Color.LightSteelBlue
        });
        return tab;
    }

    private TabPage BuildNarration()
    {
        var tab = new TabPage("2. Metin → Seslendirme")
        {
            BackColor = BackgroundColor, ForeColor = TextColor
        };
        tab.Controls.Add(Caption("SESİN OKUMASINI İSTEDİĞİN METNİ YAPIŞTIR", 20, 18, 900));
        _script.Location = new Point(20, 52);
        _script.Size = new Size(1170, 430);
        _script.Multiline = true;
        _script.ScrollBars = ScrollBars.Both;
        _script.AcceptsReturn = true;
        _script.WordWrap = true;
        _script.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _script.BackColor = PanelColor;
        _script.ForeColor = TextColor;
        _script.Font = new Font("Segoe UI", 11);
        tab.Controls.Add(_script);

        tab.Controls.Add(Caption("Windows ses seçimi:", 20, 507));
        _voices.Location = new Point(190, 506);
        _voices.Size = new Size(300, 30);
        _voices.DropDownStyle = ComboBoxStyle.DropDownList;
        _voices.Items.Add("Windows varsayılan sesi");
        _voices.SelectedIndex = 0;
        tab.Controls.Add(_voices);
        _refreshVoices.Text = "Sesleri Listele";
        _refreshVoices.Location = new Point(510, 504);
        _refreshVoices.Size = new Size(160, 34);
        _refreshVoices.Click += async (_, _) => await RefreshVoicesAsync();
        tab.Controls.Add(_refreshVoices);

        tab.Controls.Add(Caption("Konuşma hızı (-10 ... +10):", 20, 555, 260));
        _voiceRate.Minimum = -10;
        _voiceRate.Maximum = 10;
        _voiceRate.Location = new Point(285, 553);
        _voiceRate.Size = new Size(95, 30);
        tab.Controls.Add(_voiceRate);

        _generateSpeech.Text = "🎙 Metni WAV Olarak Seslendir";
        _generateSpeech.Location = new Point(20, 610);
        _generateSpeech.Size = new Size(355, 46);
        _generateSpeech.BackColor = Accent;
        _generateSpeech.ForeColor = Color.White;
        _generateSpeech.Click += async (_, _) => await GenerateSpeechAsync();
        tab.Controls.Add(_generateSpeech);

        _ttsStatus.Text = "Windows'ta yüklü seslerle çevrimdışı çalışır. Türkçe sesin Windows'ta yüklü olması gerekir.";
        _ttsStatus.Location = new Point(390, 615);
        _ttsStatus.Size = new Size(760, 70);
        _ttsStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _ttsStatus.ForeColor = Color.LightSteelBlue;
        tab.Controls.Add(_ttsStatus);
        return tab;
    }

    private TabPage BuildOutput()
    {
        var tab = new TabPage("3. Ses, Proje ve MP4")
        {
            BackColor = BackgroundColor, ForeColor = TextColor
        };
        tab.Controls.Add(Caption("SES KANALLARI", 20, 20, 900));

        tab.Controls.Add(Caption("Arka plan müziği", 20, 69));
        SetupPathBox(_musicPath, 255, 67, 640, tab);
        tab.Controls.Add(Action("Müzik Seç", 914, 64, 170, (_, _) => BrowseAudio(_musicPath)));

        tab.Controls.Add(Caption("Seslendirme dosyası", 20, 125));
        SetupPathBox(_narrationPath, 255, 123, 640, tab);
        tab.Controls.Add(Action("Ses Seç", 914, 120, 170, (_, _) => BrowseAudio(_narrationPath)));

        tab.Controls.Add(Caption("Müzik ses düzeyi (%)", 20, 184));
        _musicLevel.Minimum = 0; _musicLevel.Maximum = 200; _musicLevel.Value = 20;
        _musicLevel.Location = new Point(255, 182); _musicLevel.Size = new Size(140, 28);
        tab.Controls.Add(_musicLevel);

        tab.Controls.Add(Caption("Seslendirme düzeyi (%)", 450, 184));
        _narrationLevel.Minimum = 0; _narrationLevel.Maximum = 200; _narrationLevel.Value = 100;
        _narrationLevel.Location = new Point(690, 182); _narrationLevel.Size = new Size(140, 28);
        tab.Controls.Add(_narrationLevel);

        tab.Controls.Add(Caption("VİDEO AYARLARI", 20, 244, 900));
        tab.Controls.Add(Caption("Çıktı oranı:", 20, 289));
        _aspect.Location = new Point(255, 284);
        _aspect.Size = new Size(220, 32);
        _aspect.DropDownStyle = ComboBoxStyle.DropDownList;
        _aspect.Items.AddRange(new object[]
        {
            new Option<AspectRatio>("16:9 — YouTube", AspectRatio.Landscape16x9),
            new Option<AspectRatio>("9:16 — Shorts/Reels", AspectRatio.Portrait9x16),
            new Option<AspectRatio>("1:1 — Kare", AspectRatio.Square1x1)
        });
        _aspect.SelectedIndex = 0;
        tab.Controls.Add(_aspect);

        tab.Controls.Add(Caption("FPS:", 515, 289, 70));
        _fps.Location = new Point(575, 284); _fps.Size = new Size(130, 30);
        _fps.DropDownStyle = ComboBoxStyle.DropDownList;
        _fps.Items.AddRange(new object[] { "24", "30", "60" });
        _fps.SelectedIndex = 1;
        tab.Controls.Add(_fps);

        _fade.Text = "Fade geçişleri";
        _fade.Checked = true;
        _fade.Location = new Point(765, 284);
        _fade.Size = new Size(230, 35);
        tab.Controls.Add(_fade);

        tab.Controls.Add(Caption("PROJE DOSYASI", 20, 354, 900));
        tab.Controls.Add(Action("Projeyi Kaydet (.json)", 20, 394, 260, async (_, _) => await SaveProjectAsync()));
        tab.Controls.Add(Action("Projeyi Aç (.json)", 300, 394, 240, async (_, _) => await OpenProjectAsync()));

        _render.Text = "🎬 MP4 FİLMİ OLUŞTUR";
        _render.Location = new Point(20, 475);
        _render.Size = new Size(500, 53);
        _render.BackColor = Accent; _render.ForeColor = Color.White;
        _render.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _render.Click += async (_, _) => await RenderAsync();
        tab.Controls.Add(_render);

        _cancel.Text = "İptal";
        _cancel.Location = new Point(540, 475);
        _cancel.Size = new Size(160, 53);
        _cancel.Enabled = false;
        _cancel.Click += (_, _) => _operation?.Cancel();
        tab.Controls.Add(_cancel);

        _progress.Location = new Point(20, 552);
        _progress.Size = new Size(900, 26);
        _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        tab.Controls.Add(_progress);

        _status.Location = new Point(20, 603);
        _status.Size = new Size(1060, 60);
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _status.Text = "Hazır.";
        _status.ForeColor = Color.LightSteelBlue;
        tab.Controls.Add(_status);
        return tab;
    }

    private static void SetupPathBox(TextBox box, int x, int y, int width, Control container)
    {
        box.Location = new Point(x, y);
        box.Size = new Size(width, 30);
        box.ReadOnly = true;
        box.BackColor = PanelColor; box.ForeColor = TextColor;
        container.Controls.Add(box);
    }

    private void AddMedia(bool video)
    {
        using var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Title = video ? "Videoları seçin" : "Fotoğrafları seçin",
            Filter = video
                ? "Videolar|*.mp4;*.mov;*.avi;*.mkv;*.webm;*.m4v|Tüm dosyalar|*.*"
                : "Görseller|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Tüm dosyalar|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var list = _project.Clips.ToList();
        foreach (string path in dlg.FileNames)
        {
            list.Add(new StudioClip
            {
                SourcePath = path,
                Kind = video ? StudioMediaKind.Video : StudioMediaKind.Image,
                DurationSeconds = video ? 10 : 5
            });
        }
        _project = _project with { Clips = list };
        RefreshClips(list.Count - 1);
    }

    private void RefreshClips(int selection)
    {
        _updating = true;
        _clips.BeginUpdate();
        _clips.Items.Clear();
        foreach (var (clip, index) in _project.Clips.Select((item, i) => (item, i)))
            _clips.Items.Add((index + 1).ToString("D2") + " | "
                + (clip.Kind == StudioMediaKind.Video ? "VIDEO" : "GÖRSEL")
                + " | " + Path.GetFileName(clip.SourcePath)
                + " | " + clip.DurationSeconds.ToString("0.0") + " sn");
        _clips.EndUpdate();
        _updating = false;
        _clips.SelectedIndex = selection >= 0 && selection < _clips.Items.Count ? selection : -1;
        if (_clips.SelectedIndex < 0) LoadSelected();
    }

    private void LoadSelected()
    {
        if (_updating) return;
        int index = _clips.SelectedIndex;
        _preview.Image?.Dispose();
        _preview.Image = null;
        bool selected = index >= 0 && index < _project.Clips.Count;
        _duration.Enabled = selected;
        _trimStart.Enabled = selected;
        _clipMotion.Enabled = selected;
        _openClip.Enabled = selected && _project.Clips[index].Kind == StudioMediaKind.Video;
        if (!selected)
        {
            _previewInfo.Text = "Önizleme için bir sahne seçin.";
            return;
        }

        var clip = _project.Clips[index];
        _updating = true;
        _duration.Value = Math.Clamp((decimal)clip.DurationSeconds, _duration.Minimum, _duration.Maximum);
        _trimStart.Value = Math.Clamp((decimal)clip.TrimStartSeconds, _trimStart.Minimum, _trimStart.Maximum);
        _trimStart.Enabled = clip.Kind == StudioMediaKind.Video;
        _clipMotion.Enabled = clip.Kind == StudioMediaKind.Image;
        for (int i = 0; i < _clipMotion.Items.Count; i++)
            if (((Option<MotionType>)_clipMotion.Items[i]!).Value == clip.Motion)
                _clipMotion.SelectedIndex = i;
        _updating = false;

        _previewInfo.Text = Path.GetFileName(clip.SourcePath) + Environment.NewLine
            + (clip.Kind == StudioMediaKind.Image
                ? "Fotoğraf: Pan/Zoom animasyonu ile videoya dönüştürülür."
                : "Video: kesilecek başlangıç ve süreyi ayarla. Oynatmak için alttaki düğmeyi kullan.");
        if (clip.Kind == StudioMediaKind.Image)
        {
            try
            {
                using var stream = File.OpenRead(clip.SourcePath);
                using var img = Image.FromStream(stream);
                _preview.Image = new Bitmap(img);
            }
            catch (Exception ex) { _previewInfo.Text = "Fotoğraf açılamadı: " + ex.Message; }
        }
    }

    private void ApplySelected()
    {
        int index = _clips.SelectedIndex;
        if (index < 0) return;
        var items = _project.Clips.ToList();
        var selected = items[index];
        items[index] = selected with
        {
            DurationSeconds = (double)_duration.Value,
            TrimStartSeconds = selected.Kind == StudioMediaKind.Video ? (double)_trimStart.Value : 0,
            Motion = ((Option<MotionType>)_clipMotion.SelectedItem!).Value
        };
        _project = _project with { Clips = items };
        RefreshClips(index);
    }

    private void RemoveSelected()
    {
        int index = _clips.SelectedIndex;
        if (index < 0) return;
        var list = _project.Clips.ToList();
        list.RemoveAt(index);
        _project = _project with { Clips = list };
        RefreshClips(Math.Min(index, list.Count - 1));
    }

    private void MoveSelected(int change)
    {
        int index = _clips.SelectedIndex;
        int other = index + change;
        if (index < 0 || other < 0 || other >= _project.Clips.Count) return;
        var list = _project.Clips.ToList();
        (list[index], list[other]) = (list[other], list[index]);
        _project = _project with { Clips = list };
        RefreshClips(other);
    }

    private void OpenSelectedVideo()
    {
        int index = _clips.SelectedIndex;
        if (index < 0) return;
        try
        {
            Process.Start(new ProcessStartInfo(_project.Clips[index].SourcePath) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Video oynatma hatası"); }
    }

    private async Task RefreshVoicesAsync()
    {
        _refreshVoices.Enabled = false;
        _ttsStatus.Text = "Windows sesleri aranıyor...";
        try
        {
            var voices = await SpeechService.GetVoicesAsync();
            _voices.Items.Clear();
            _voices.Items.Add("Windows varsayılan sesi");
            foreach (var v in voices) _voices.Items.Add(v);
            _voices.SelectedIndex = 0;
            _ttsStatus.Text = voices.Count + " ses bulundu. Türkçe konuşma için Türkçe bir Windows sesi seçin.";
        }
        catch (Exception ex) { ShowError(ex, "Sesler listelenemedi"); }
        finally { _refreshVoices.Enabled = true; }
    }

    private async Task GenerateSpeechAsync()
    {
        if (string.IsNullOrWhiteSpace(_script.Text))
        {
            MessageBox.Show(this, "Önce seslendirilecek metni yazın."); return;
        }
        using var save = new SaveFileDialog
        {
            Filter = "WAV sesi|*.wav", FileName = "TROCKI_Seslendirme.wav",
            Title = "Seslendirmeyi kaydet"
        };
        if (save.ShowDialog(this) != DialogResult.OK) return;
        _operation = new CancellationTokenSource();
        _generateSpeech.Enabled = false; _cancel.Enabled = true;
        _ttsStatus.Text = "Seslendirme hazırlanıyor...";
        try
        {
            var voice = _voices.SelectedIndex <= 0 ? null : _voices.SelectedItem?.ToString();
            await SpeechService.SynthesizeAsync(_script.Text, save.FileName, voice,
                (int)_voiceRate.Value, _operation.Token);
            _project = _project with { NarrationPath = save.FileName };
            _narrationPath.Text = save.FileName;
            _ttsStatus.Text = "WAV oluşturuldu ve videonun seslendirme kanalına eklendi: " + save.FileName;
        }
        catch (OperationCanceledException) { _ttsStatus.Text = "İptal edildi."; }
        catch (Exception ex) { ShowError(ex, "Seslendirme oluşturulamadı"); }
        finally
        {
            _operation.Dispose(); _operation = null;
            _generateSpeech.Enabled = true; _cancel.Enabled = false;
        }
    }

    private void BrowseAudio(TextBox target)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Ses dosyaları|*.wav;*.mp3;*.m4a;*.aac;*.flac;*.ogg|Tüm dosyalar|*.*"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) target.Text = dlg.FileName;
    }

    private StudioProject CaptureProject() => _project with
    {
        MusicPath = string.IsNullOrWhiteSpace(_musicPath.Text) ? null : _musicPath.Text,
        NarrationPath = string.IsNullOrWhiteSpace(_narrationPath.Text) ? null : _narrationPath.Text,
        MusicVolume = (double)_musicLevel.Value / 100,
        NarrationVolume = (double)_narrationLevel.Value / 100,
        Aspect = ((Option<AspectRatio>)_aspect.SelectedItem!).Value,
        Fps = int.Parse(_fps.SelectedItem!.ToString()!),
        Fade = _fade.Checked,
        NarrationText = _script.Text
    };

    private void RefreshProject()
    {
        RefreshClips(_project.Clips.Count > 0 ? 0 : -1);
        _musicPath.Text = _project.MusicPath ?? "";
        _narrationPath.Text = _project.NarrationPath ?? "";
        _script.Text = _project.NarrationText;
        _musicLevel.Value = Math.Clamp((decimal)(_project.MusicVolume * 100), 0, 200);
        _narrationLevel.Value = Math.Clamp((decimal)(_project.NarrationVolume * 100), 0, 200);
        for (int i = 0; i < _aspect.Items.Count; i++)
            if (((Option<AspectRatio>)_aspect.Items[i]!).Value == _project.Aspect)
                _aspect.SelectedIndex = i;
        _fps.SelectedItem = _project.Fps.ToString();
        _fade.Checked = _project.Fade;
    }

    private async Task SaveProjectAsync()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "TROÇKİ proje dosyası|*.json",
            FileName = _projectFile is null ? "MotionStudio_Proje.json" : Path.GetFileName(_projectFile)
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _project = CaptureProject();
            await _project.SaveAsync(dlg.FileName);
            _projectFile = dlg.FileName;
            _status.Text = "Proje kaydedildi: " + dlg.FileName;
        }
        catch (Exception ex) { ShowError(ex, "Proje kaydedilemedi"); }
    }

    private async Task OpenProjectAsync()
    {
        using var dlg = new OpenFileDialog { Filter = "TROÇKİ proje dosyası|*.json" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _project = await StudioProject.LoadAsync(dlg.FileName);
            _projectFile = dlg.FileName;
            RefreshProject();
            _status.Text = "Proje açıldı: " + dlg.FileName;
        }
        catch (Exception ex) { ShowError(ex, "Proje açılamadı"); }
    }

    private async Task RenderAsync()
    {
        StudioProject project = CaptureProject();
        try { project.Validate(); }
        catch (Exception ex) { ShowError(ex, "Eksik medya veya ayarlar"); return; }

        using var dlg = new SaveFileDialog
        {
            Filter = "MP4 video|*.mp4", FileName = "TROCKI_Motion_Studio_PRO.mp4"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        string destination = Path.GetFullPath(dlg.FileName);
        if (project.Clips.Any(c => string.Equals(Path.GetFullPath(c.SourcePath), destination, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "Çıktı yolu kaynak videonun üzerine yazamaz."); return;
        }

        _operation = new CancellationTokenSource();
        _render.Enabled = false; _cancel.Enabled = true; _progress.Value = 0;
        _status.Text = "Film hazırlanıyor...";
        try
        {
            _project = project;
            var renderer = new StudioRenderer(VideoRenderer.LocateFfmpeg(AppContext.BaseDirectory));
            var progress = new Progress<double>(p => _progress.Value = Math.Clamp((int)Math.Round(p * 100), 0, 100));
            await renderer.RenderAsync(project, destination, progress, _operation.Token);
            _status.Text = "MP4 hazır: " + destination;
            MessageBox.Show(this, "Video başarıyla oluşturuldu:\n" + destination, "TROÇKİ Motion Studio PRO");
        }
        catch (OperationCanceledException) { _status.Text = "Video oluşturma iptal edildi."; }
        catch (Exception ex) { ShowError(ex, "Video oluşturulamadı"); }
        finally
        {
            _operation.Dispose(); _operation = null;
            _render.Enabled = true; _cancel.Enabled = false;
        }
    }

    private void ShowError(Exception ex, string title)
    {
        _status.Text = title + ": " + ex.Message;
        _ttsStatus.Text = title + ": " + ex.Message;
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
