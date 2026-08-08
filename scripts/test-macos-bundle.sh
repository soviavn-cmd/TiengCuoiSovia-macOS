#!/usr/bin/env bash
set -euo pipefail

DMG_PATH="${1:?Usage: test-macos-bundle.sh <dmg-path>}"
EXPECTED_MEDIA=120
MOUNT_POINT="${RUNNER_TEMP:-/tmp}/sovia-mount"
APP="$MOUNT_POINT/Tieng Cuoi Sovia.app"
LOG_FILE="${RUNNER_TEMP:-/tmp}/sovia-launch.log"

cleanup() {
  if [[ -n "${APP_PID:-}" ]] && kill -0 "$APP_PID" 2>/dev/null; then
    kill "$APP_PID" 2>/dev/null || true
    wait "$APP_PID" 2>/dev/null || true
  fi
  hdiutil detach "$MOUNT_POINT" -quiet 2>/dev/null || true
}
trap cleanup EXIT

rm -rf "$MOUNT_POINT"
mkdir -p "$MOUNT_POINT"
hdiutil attach "$DMG_PATH" -nobrowse -readonly -mountpoint "$MOUNT_POINT" -quiet

test -d "$APP"
test -x "$APP/Contents/MacOS/TiengCuoiSovia"
plutil -lint "$APP/Contents/Info.plist"
codesign --verify --deep --strict --verbose=2 "$APP"

MEDIA_ROOT="$APP/Contents/MacOS/Media"
MEDIA_COUNT=$(find "$MEDIA_ROOT" -type f \( -iname '*.mp3' -o -iname '*.wav' -o -iname '*.m4a' -o -iname '*.aac' \) | wc -l | tr -d ' ')
[[ "$MEDIA_COUNT" == "$EXPECTED_MEDIA" ]]

FAILED=0
while IFS= read -r -d '' AUDIO_FILE; do
  if ! afinfo "$AUDIO_FILE" >/dev/null 2>&1; then
    echo "Invalid CoreAudio file: $AUDIO_FILE"
    FAILED=$((FAILED + 1))
  fi
done < <(find "$MEDIA_ROOT" -type f \( -iname '*.mp3' -o -iname '*.wav' -o -iname '*.m4a' -o -iname '*.aac' \) -print0)
[[ "$FAILED" == 0 ]]

grep -q 'Luôn Hiện' MainWindow.axaml
grep -q 'ÂM LƯỢNG' MainWindow.axaml
grep -q 'EFFECT1' MainWindow.axaml
grep -q 'MUSIC2' MainWindow.axaml
grep -q 'EDIT' MainWindow.axaml
grep -q 'Topmost' MainWindow.axaml.cs
grep -q 'OpenFilePickerAsync' EditSoundDialog.cs
grep -q '/usr/bin/afplay' Services.cs

"$APP/Contents/MacOS/TiengCuoiSovia" --self-test

"$APP/Contents/MacOS/TiengCuoiSovia" >"$LOG_FILE" 2>&1 &
APP_PID=$!
sleep 8
if ! kill -0 "$APP_PID" 2>/dev/null; then
  echo "Application exited during launch smoke test"
  cat "$LOG_FILE"
  exit 1
fi

echo "PASS: mounted DMG, verified app signature, launched app, and validated all $MEDIA_COUNT audio files."
