# TROÇKİ Motion Studio

Görsellerden sinematik videolar üreten masaüstü video stüdyosu.

- **Teknoloji:** C# · .NET 8 · Windows Forms · FFmpeg
- **Özellikler:** Çoklu görsel seçimi ve önizleme, Zoom In/Out, dört yönlü Pan, sinematik Zoom + Pan, hız ayarı, fade geçişi, 16:9 / 9:16 / 1:1 oranları, 24/30/60 FPS, tek MP4 çıktısı (H.264)

## Proje yapısı

| Proje | Açıklama |
|---|---|
| `src/MotionStudio.Core` | Hareket, geçiş ve FFmpeg komutlarının üretimi; FFmpeg'i çalıştırma |
| `src/MotionStudio.Cli` | Komut satırı aracı (`motionstudio`) |
| `src/MotionStudio.App` | Windows Forms arayüzü (`TrockiMotionStudio.exe`) |
| `scripts/smoke-test.sh` | Gerçek FFmpeg ile uçtan uca test |
| `docs/` | Tanıtım sunumu ve afiş |

## Gereksinimler

- .NET 8 SDK
- FFmpeg (`ffmpeg.exe`): `ffmpeg/` klasörüne koyun ya da PATH'e ekleyin.
  Windows için: https://www.gyan.dev/ffmpeg/builds/

## Derleme ve çalıştırma

```bash
dotnet build TrockiMotionStudio.sln -c Release
dotnet run --project src/MotionStudio.App
```

Komut satırı kullanımı:

```bash
dotnet run --project src/MotionStudio.Cli -- --out video.mp4 --seconds 5 --motion cinematic \
  --speed slow --aspect 16:9 --fps 30 foto1.jpg foto2.jpg foto3.jpg
```

Seçenekler: `--aspect 16:9|9:16|1:1`, `--seconds`, `--fps 24|30|60`,
`--motion none|zoom-in|zoom-out|pan-lr|pan-rl|pan-tb|pan-bt|cinematic`,
`--speed very-slow|slow|normal|fast`, `--fade on|off`, `--ffmpeg yol`

## Nasıl çalışır

1. Her görsel, seçilen oran ve FPS'te bir MP4 klibe dönüştürülür (`zoompan` filtresi ile hareket).
2. Fade geçişi açıksa her klibin başına açılma, sonuna kararma uygulanır.
3. Klipler aynı kodlama ayarlarıyla üretildiği için `concat` ile kayıpsız birleştirilir.

**Hız** hareketin klip içindeki tamamlanma oranını belirler: Çok Yavaş klibin yarısında, Hızlı ise
klibin üçte ikisinde hareketi bitirir. Yavaş ve çok yavaş seçeneklerde hareket klip sonunda
tamamlanmaz.

## Test

```bash
bash scripts/smoke-test.sh
```

Bu betik gerçek FFmpeg ile sekiz hareket türünü, dikey ve kare oranlarını ve FPS/süre değerlerini kontrol eder.

## Bilinen sınırlamalar

- Görsel kırpılarak doldurulur; bazı oranlarda kadraj kesilebilir (yol haritasındaki "Güvenli Fit + Pan" bunu çözecek).
- Ses / müzik ekleme henüz yok.
- Geçiş yalnızca her klip başı ve sonu için fade; klipler arası crossfade yok.

## Yol haritası

1. Güvenli Fit + Pan
2. Gerçek crossfade
3. Her görsele ayrı süre ve hareket
4. Ses / müzik, zaman çizelgesi, proje kaydetme
5. AI Video: görsel + prompt → hareketli video

Sunum ve afiş: `docs/` klasöründe.

Hazırlayan: TROÇKİ
