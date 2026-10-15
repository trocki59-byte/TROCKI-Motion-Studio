#!/usr/bin/env bash
# Çekirdek motoru gerçek FFmpeg ile test eder: test görselleri üretir, her hareket türü için
# video oluşturur ve süre/çözünürlük/FPS değerlerini doğrular.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK="$(mktemp -d)"
CLI_PROJ="$ROOT/src/MotionStudio.Cli/MotionStudio.Cli.csproj"
# NO_RESTORE=1 ortamda paket geri yüklemesini atlar (önceden restore edilmişse).
dotnet build "$CLI_PROJ" -c Release -nologo ${NO_RESTORE:+--no-restore} >/dev/null
CLI="dotnet $ROOT/src/MotionStudio.Cli/bin/Release/net8.0/motionstudio.dll"

ffmpeg -hide_banner -loglevel error -y -f lavfi -i "testsrc2=size=1600x1000:rate=1" -frames:v 1 "$WORK/a.png"
ffmpeg -hide_banner -loglevel error -y -f lavfi -i "mandelbrot=size=1200x1600" -frames:v 1 "$WORK/b.png"

fail=0
check() { # dosya beklenen_sn beklenen_genişlik beklenen_yükseklik
  local f="$1" dur w h
  dur=$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$f")
  IFS=, read -r w h < <(ffprobe -v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 "$f")
  if awk -v d="$dur" -v e="$2" 'BEGIN{exit !((d-e)^2<0.25)}' && [[ "$w" == "$3" && "$h" == "$4" ]]; then
    echo "OK   $(basename "$f")  ${dur}s  ${w}x${h}"
  else
    echo "FAIL $(basename "$f")  ${dur}s  ${w}x${h} (beklenen ${2}s ${3}x${4})"; fail=1
  fi
}

for m in none zoom-in zoom-out pan-lr pan-rl pan-tb pan-bt cinematic; do
  $CLI --out "$WORK/$m.mp4" --seconds 2 --motion "$m" --speed fast "$WORK/a.png" >/dev/null
  check "$WORK/$m.mp4" 2 1920 1080
done

$CLI --out "$WORK/dikey.mp4" --seconds 3 --aspect 9:16 --fps 24 "$WORK/a.png" "$WORK/b.png" >/dev/null
check "$WORK/dikey.mp4" 6 1080 1920

$CLI --out "$WORK/kare.mp4" --seconds 2 --aspect 1:1 --fps 60 --fade off "$WORK/a.png" >/dev/null
check "$WORK/kare.mp4" 2 1080 1080

rm -rf "$WORK"
exit $fail
