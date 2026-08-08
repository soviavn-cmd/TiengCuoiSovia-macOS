#!/usr/bin/env bash
set -euo pipefail

DMG_PATH="${1:?Usage: test-macos-bundle.sh <dmg-path>}"
EXPECTED_MEDIA=120
MOUNT_POINT="${RUNNER_TEMP:-/tmp}/sovia-mount"
APP="$MOUNT_POINT/Tieng Cuoi Sovia.app"
LOG_FILE="${RUNNER_TEMP:-/tmp}/sovia-launch.log"
REQUIRED_CLEAN_ROUNDS=20

cleanup() {
  if [[ -n "${APP_PID:-}" ]] && kill -0 "$APP_PID" 2>/dev/null; then
    kill "$APP_PID" 2>/dev/null || true
    wait "$APP_PID" 2>/dev/null || true
  fi
  hdiutil detach "$MOUNT_POINT" -quiet 2>/dev/null || true
}
trap cleanup EXIT

for ROUND in $(seq 1 "$REQUIRED_CLEAN_ROUNDS"); do
  rm -rf "$MOUNT_POINT"
  mkdir -p "$MOUNT_POINT"
  hdiutil attach "$DMG_PATH" -nobrowse -readonly -mountpoint "$MOUNT_POINT" -quiet

  test -d "$APP"
  test -x "$APP/Contents/MacOS/TiengCuoiSovia"
  test -x "$APP/Contents/MacOS/SoviaAudioPlayer"
  plutil -lint "$APP/Contents/Info.plist" >/dev/null
  codesign --verify --deep --strict "$APP"

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

  grep -q 'Width="342" Height="680"' MainWindow.axaml
  grep -q 'CornerRadius="17"' MainWindow.axaml
  grep -q 'RowDefinitions="32,42,38,1,\*,22"' MainWindow.axaml
  grep -q 'Luôn Hiện' MainWindow.axaml
  grep -q 'ÂM LƯỢNG' MainWindow.axaml
  grep -q 'EFFECT1' MainWindow.axaml
  grep -q 'MUSIC2' MainWindow.axaml
  grep -q 'EDIT' MainWindow.axaml
  grep -q 'Topmost' MainWindow.axaml.cs
  grep -q 'OpenFilePickerAsync' EditSoundDialog.cs
  grep -q 'SoviaAudioPlayer' Services.cs

  "$APP/Contents/MacOS/TiengCuoiSovia" --self-test

  "$APP/Contents/MacOS/TiengCuoiSovia" >"$LOG_FILE" 2>&1 &
  APP_PID=$!
  sleep 3
  if ! kill -0 "$APP_PID" 2>/dev/null; then
    echo "Application exited during launch smoke test in round $ROUND"
    cat "$LOG_FILE"
    exit 1
  fi
  kill "$APP_PID" 2>/dev/null || true
  wait "$APP_PID" 2>/dev/null || true
  APP_PID=""
  hdiutil detach "$MOUNT_POINT" -quiet
  echo "AUDIT_ROUND_PASS: $ROUND/$REQUIRED_CLEAN_ROUNDS"
done

echo "PASS: 20 consecutive strict rounds; DMG mounted, app launched, features exercised, and all 120 audio files validated each round."
