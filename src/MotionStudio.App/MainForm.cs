using MotionStudio.Core;

namespace MotionStudio.App;

/// <summary>Tek ekranlı arayüz: görsel listesi, önizleme, video ayarları ve üretim.</summary>
public sealed class MainForm : Form
{
    private sealed record Option<T>(string Text, T Value)
    {
        public override string ToString() => Text;
    }

    private static readonly Color Bg = Color.FromArgb(23, 18, 14);
    private static readonly Color Panel = Color.FromArgb(36, 29, 23);
    private static readonly Color Cream = Color.FromArgb(244, 235, 221);
    private static readonly Color Gold = Color.FromArgb(217, 164, 65);
    private static readonly Color Muted = Color.FromArgb(168, 154, 136);

    private readonly ListBox _list = new();
    private readonly PictureBox _preview = new();
    private readonly NumericUpDown _seconds = new();
    private readonly ComboBox _motion = new();
    private readonly ComboBox _speed = new();
    private readonly ComboBox _fps = new();
    private readonly ComboBox _aspect = new();
    private readonly CheckBox _fade = new();
    private readonly Button _add = new();
    private readonly Button _remove = new();
    private readonly Button _create = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _status = new();

    private readonly List<string> _images = new();

    public MainForm()
    {
        Text = "TROÇKİ Motion Studio";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 560);
        Size = new Size(1000, 620);
        BackColor = Bg;
        ForeColor = Cream;
        Font = new Font("Segoe UI", 9.5f);

        BuildLayout();
        WireEvents();
        UpdateButtons();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "TROÇKİ Motion Studio",
            Font = new Font("Georgia", 20f, FontStyle.Bold),
            ForeColor = Cream,
            Location = new Point(20, 14),
            AutoSize = true
        };
        var subtitle = new Label
        {
            Text = "Görsellerden sinematik videolara",
            ForeColor = Gold,
            Location = new Point(22, 58),
            AutoSize = true
        };

        _list.DisplayMember = "";
        _list.BackColor = Panel;
        _list.ForeColor = Cream;
        _list.BorderStyle = BorderStyle.None;
        _list.Location = new Point(20, 100);
        _list.Size = new Size(260, 330);
        _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        _add.Text = "Görsel Ekle";
        _add.Location = new Point(20, 440);
        _add.Size = new Size(125, 36);
        _add.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        StyleButton(_add, Color.FromArgb(59, 125, 221));

        _remove.Text = "Seçilenleri Kaldır";
        _remove.Location = new Point(155, 440);
        _remove.Size = new Size(125, 36);
        _remove.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        StyleButton(_remove, Color.FromArgb(192, 57, 43));

        _preview.Location = new Point(300, 100);
        _preview.Size = new Size(400, 225);
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.BackColor = Color.Black;
        _preview.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        var settingsPanel = new Panel
        {
            Location = new Point(720, 100),
            Size = new Size(250, 330),
            BackColor = Panel,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        int y = 14;
        AddSetting(settingsPanel, "Süre (sn)", _seconds, ref y);
        _seconds.Minimum = 1;
        _seconds.Maximum = 30;
        _seconds.Value = 5;
        _seconds.DecimalPlaces = 0;

        AddSetting(settingsPanel, "Hareket", _motion, ref y);
        _motion.Items.AddRange(new object[]
        {
            new Option<MotionType>("Sabit", MotionType.None),
            new Option<MotionType>("Zoom In", MotionType.ZoomIn),
            new Option<MotionType>("Zoom Out", MotionType.ZoomOut),
            new Option<MotionType>("Pan Sol → Sağ", MotionType.PanLeftToRight),
            new Option<MotionType>("Pan Sağ → Sol", MotionType.PanRightToLeft),
            new Option<MotionType>("Pan Yukarı → Aşağı", MotionType.PanTopToBottom),
            new Option<MotionType>("Pan Aşağı → Yukarı", MotionType.PanBottomToTop),
            new Option<MotionType>("Sinematik Zoom + Pan", MotionType.Cinematic)
        });
        _motion.SelectedIndex = 1;

        AddSetting(settingsPanel, "Hız", _speed, ref y);
        _speed.Items.AddRange(new object[]
        {
            new Option<SpeedLevel>("Çok Yavaş", SpeedLevel.VerySlow),
            new Option<SpeedLevel>("Yavaş", SpeedLevel.Slow),
            new Option<SpeedLevel>("Normal", SpeedLevel.Normal),
            new Option<SpeedLevel>("Hızlı", SpeedLevel.Fast)
        });
        _speed.SelectedIndex = 2;

        AddSetting(settingsPanel, "FPS", _fps, ref y);
        _fps.Items.AddRange(new object[]
        {
            new Option<int>("24", 24), new Option<int>("30", 30), new Option<int>("60", 60)
        });
        _fps.SelectedIndex = 1;

        AddSetting(settingsPanel, "Video oranı", _aspect, ref y);
        _aspect.Items.AddRange(new object[]
        {
            new Option<AspectRatio>("16:9 (Yatay)", AspectRatio.Landscape16x9),
            new Option<AspectRatio>("9:16 (Dikey)", AspectRatio.Portrait9x16),
            new Option<AspectRatio>("1:1 (Kare)", AspectRatio.Square1x1)
        });
        _aspect.SelectedIndex = 0;

        _fade.Text = "Fade geçişi";
        _fade.Checked = true;
        _fade.ForeColor = Cream;
        _fade.Location = new Point(14, y);
        _fade.AutoSize = true;
        settingsPanel.Controls.Add(_fade);

        _create.Text = "Videoyu Oluştur";
        _create.Location = new Point(300, 340);
        _create.Size = new Size(400, 40);
        _create.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        StyleButton(_create, Color.FromArgb(59, 125, 221));

        _progress.Location = new Point(300, 395);
        _progress.Size = new Size(400, 16);
        _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _progress.Maximum = 100;

        _status.Text = "Hazır. Görsel ekleyin.";
        _status.ForeColor = Muted;
        _status.Location = new Point(300, 420);
        _status.Size = new Size(670, 40);
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange(new Control[]
        {
            title, subtitle, _list, _add, _remove, _preview, settingsPanel, _create, _progress, _status
        });
    }

    private static void AddSetting(Panel panel, string label, Control input, ref int y)
    {
        panel.Controls.Add(new Label { Text = label, ForeColor = Muted, Location = new Point(14, y + 4), AutoSize = true });
        input.Location = new Point(110, y);
        input.Size = new Size(126, 24);
        panel.Controls.Add(input);
        y += 36;
    }

    private static void StyleButton(Button b, Color back)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = back;
        b.ForeColor = Color.White;
        b.Cursor = Cursors.Hand;
    }

    private void WireEvents()
    {
        _add.Click += (_, _) => AddImages();
        _remove.Click += (_, _) => RemoveSelected();
        _create.Click += async (_, _) => await CreateVideoAsync();
        _list.SelectedIndexChanged += (_, _) => ShowPreview();
        _list.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) RemoveSelected(); };
    }

    private void AddImages()
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Görseller|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Tüm dosyalar|*.*",
            Title = "Görselleri seçin"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        foreach (var file in dialog.FileNames)
        {
            if (_images.Contains(file)) continue;
            _images.Add(file);
            _list.Items.Add(Path.GetFileName(file));
        }
        if (_list.SelectedIndex < 0 && _list.Items.Count > 0) _list.SelectedIndex = 0;
        UpdateButtons();
    }

    private void RemoveSelected()
    {
        var indices = _list.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
        foreach (var i in indices)
        {
            _images.RemoveAt(i);
            _list.Items.RemoveAt(i);
        }
        ShowPreview();
        UpdateButtons();
    }

    private void ShowPreview()
    {
        _preview.Image?.Dispose();
        _preview.Image = null;
        if (_list.SelectedIndex >= 0 && _list.SelectedIndex < _images.Count)
        {
            try
            {
                using var stream = File.OpenRead(_images[_list.SelectedIndex]);
                using var img = Image.FromStream(stream);
                _preview.Image = new Bitmap(img);
            }
            catch (Exception ex)
            {
                _status.Text = "Önizleme açılamadı: " + ex.Message;
            }
        }
    }

    private VideoSettings CurrentSettings() => new()
    {
        SecondsPerImage = (double)_seconds.Value,
        Motion = ((Option<MotionType>)_motion.SelectedItem!).Value,
        Speed = ((Option<SpeedLevel>)_speed.SelectedItem!).Value,
        Fps = ((Option<int>)_fps.SelectedItem!).Value,
        Aspect = ((Option<AspectRatio>)_aspect.SelectedItem!).Value,
        Transition = _fade.Checked ? TransitionType.Fade : TransitionType.None
    };

    private async Task CreateVideoAsync()
    {
        if (_images.Count == 0) return;

        using var dialog = new SaveFileDialog
        {
            Filter = "MP4 video|*.mp4",
            FileName = "TROCKI_Motion_Studio.mp4",
            Title = "Videoyu nereye kaydedelim?"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var renderer = new VideoRenderer(VideoRenderer.LocateFfmpeg(AppContext.BaseDirectory));
        var progress = new Progress<double>(p => _progress.Value = (int)Math.Round(p * 100));

        SetBusy(true);
        _status.Text = "Oluşturuluyor…";
        try
        {
            await renderer.RenderAsync(_images.ToList(), dialog.FileName, CurrentSettings(), progress);
            _status.Text = "Tamamlandı: " + dialog.FileName;
        }
        catch (Exception ex)
        {
            _status.Text = "Hata: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Video oluşturulamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _progress.Value = 0;
        foreach (Control c in new Control[] { _add, _remove, _create, _seconds, _motion, _speed, _fps, _aspect, _fade })
            c.Enabled = !busy;
        UseWaitCursor = busy;
        if (!busy) UpdateButtons();
    }

    private void UpdateButtons()
    {
        _create.Enabled = _images.Count > 0;
        _remove.Enabled = _list.SelectedIndices.Count > 0;
    }
}
