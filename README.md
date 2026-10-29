# TROÇKİ Motion Studio PRO 🎬

**Windows masaüstünde fotoğraflar, videolar, seslendirme ve müziği tek MP4 filmde birleştiren stüdyo.**

Teknoloji: C# / .NET 8 / Windows Forms / FFmpeg. Uygulama internet gerektirmeden çalışır; seslendirme Windows'un yerel konuşma seslerini kullanır.

## PRO özellikleri

- Fotoğraf ve videoyu **sahneler halinde ekleme, silme ve ↑↓ ile yeniden sıralama**.
- Videoyu **başlangıç saniyesi ve sahne süresi** ile kırpma; hareketli fotoğraf için Zoom/Pan.
- Seslendirme metnini yazıp/yapıştırıp Windows sesleriyle WAV üretme.
- Ayrı **müzik** ve **seslendirme** kanalları, ses seviyesi ayarları.
- 16:9 (YouTube), 9:16 (Shorts/Reels), 1:1 ve 24/30/60 FPS.
- JSON proje açma/kaydetme, ilerleme ve iptal.
- MP4 (H.264 + varsa AAC ses) dışa aktarma.

### Windows EXE indirme

**Önerilen: Kurulumlu sürüm.** Başarılı GitHub Actions çalışmasında `TROCKI-Motion-Studio-PRO-Setup-Windows-x64` artifact'ini indir, ZIP'i aç ve `TROCKI-Motion-Studio-PRO-Setup.exe` programını çalıştır. Kurulum Başlat menüsü ve masaüstü kısayolu oluşturur. FFmpeg ayrıca kurulmalıdır.

**Alternatif: Taşınabilir EXE.**

1. GitHub deposundaki **Actions** sekmesine gir.
2. Başarılı **Build Windows EXE** çalışmasını seç.
3. **Artifacts → TROCKI-Motion-Studio-PRO-Windows-x64** dosyasını indir ve ZIP'i aç.
4. İçindeki **TrockiMotionStudio.exe** dosyasını çalıştır.
5. Masaüstüne kısayol oluşturmak için EXE'ye sağ tıkla → **Gönder → Masaüstü (kısayol oluştur)**.

Derleme *self-contained* tek dosya olarak yayınlanır: bilgisayara ayrıca .NET Runtime yükleme zorunluluğu yoktur.
**FFmpeg farklı bir uygulamadır ve henüz EXE içine gömülmemiştir**. Video üretimi için:
- Windows için FFmpeg'i https://www.gyan.dev/ffmpeg/builds/ adresinden edin.
- \`ffmpeg.exe\` dosyasını EXE'nin yanındaki \`ffmpeg\` klasörüne koy **veya** FFmpeg \`bin\` klasörünü PATH'e ekle.

> FFmpeg yoksa program açılır ama MP4 oluşturulamaz. Kullandığınız FFmpeg dağıtımının lisans koşullarına uyun.

### Seslendirme

1. **Metin → Seslendirme** sekmesinde metni yaz.
2. **Sesleri Listele** ile Windows'taki SAPI seslerini gör.
3. Türkçe ses yüklüyse onu seç, konuşma hızını belirle.
4. **Metni WAV Olarak Seslendir** ile konuşmayı üret.
5. Oluşan WAV, **Ses / Proje ve MP4** sekmesindeki seslendirme kanalına otomatik eklenir.

Yalnızca Windows'un klasik **SAPI / System.Speech** sesleri listelenir. Türkçe ses yoksa Windows'a uyumlu Türkçe konuşma sesi kurulması gerekir. Windows "OneCore" seslerinin tamamı klasik SAPI uygulamalarına görünmeyebilir. TTS bulut servisi veya yapay zekâ ses klonlaması değildir.

### MP4 düzenleme adımları

1. **Video Düzenleme** sekmesinde fotoğraf ve videoları ekle, sırala.
2. Bir sahneye tıkla: süreyi, video ise başlangıcını, fotoğraf ise hareket tipini ayarla ve **Uygula**.
3. **Ses, Proje ve MP4** sekmesinden arka plan müziği ekle; varsa ürettiğin WAV'ı ekle.
4. Video oranı ve FPS'i seç; **MP4 FİLMİ OLUŞTUR** düğmesine bas.

Uzun tarih anlatıları için sahnelere yüzlerce saniye süre tanımlayabilirsin. Büyük videoların işlenmesi uzun sürebilir.

## Kaynak ve derleme

\`\`\`powershell
dotnet build src/MotionStudio.App/MotionStudio.App.csproj -c Release
dotnet run --project src/MotionStudio.App
dotnet publish src/MotionStudio.App/MotionStudio.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
\`\`\`

GitHub Actions Windows üzerinde otomatik derleyip kurulum gerektirmeyen EXE çıktısını artifact olarak sunar.

## Proje yapısı

- \`src/MotionStudio.Core\`: FFmpeg hareket, klip birleştirme ve ses miks motorları; proje veri modeli.
- \`src/MotionStudio.App/ProForm.cs\`: Windows masaüstü düzenleyici.
- \`src/MotionStudio.App/SpeechService.cs\`: Windows PowerShell / System.Speech ile metinden WAV.
- \`src/MotionStudio.Cli\`: Eski komut satırı aracı (korunur).
- \`scripts/smoke-test.sh\`: Eski klip üretim testleri.

## Bu sürümün sınırları

- Yerleşik oynatıcı yok; seçili video, Windows varsayılan video oynatıcısında açılır.
- Hassas bir çok kanallı dalga biçimi zaman çizelgesi, gerçek crossfade, gelişmiş altyazı ve ses efektleri henüz yok.
- Kesme basitçe **başlangıç + süre** üzerinden yapılır; ses kanalları toplam film süresine hizalanır.
- Müzik film süresince tekrar eder, seslendirme kısa kalırsa kalan kısım sessizdir.
- Görselleri projede mutlak dosya yollarından okur; medya taşınırsa proje dosyası yeniden bağlanmalıdır.

**Hazırlayan: TROÇKİ STUDIO**
