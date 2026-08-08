using System.Diagnostics;

namespace TiengCuoiSoviaMac;

internal static class SelfTest
{
    public static async Task<int> RunAsync()
    {
        try
        {
            if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("Self-test này phải chạy trên macOS.");
            var mediaRoot = Path.Combine(AppContext.BaseDirectory, "Media");
            var files = Directory.EnumerateFiles(mediaRoot, "*", SearchOption.AllDirectories)
                .Where(path => new[] { ".mp3", ".wav", ".m4a", ".aac" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .ToArray();
            Assert(files.Length == 120, $"Expected 120 audio files, found {files.Length}.");

            foreach (var file in files)
            {
                var probeInfo = new ProcessStartInfo("/usr/bin/afinfo") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                probeInfo.ArgumentList.Add(file);
                using var probe = Process.Start(probeInfo);
                Assert(probe is not null, "Unable to start afinfo.");
                await probe!.WaitForExitAsync();
                Assert(probe.ExitCode == 0, $"CoreAudio rejected: {file}");
            }

            using (var audio = new AudioService { Volume = 35 })
            {
                audio.Play(files.OrderByDescending(path => new FileInfo(path).Length).First());
                await Task.Delay(700);
                Assert(audio.IsPlaying, "afplay did not stay active during playback test.");
                audio.Volume = 68;
                await Task.Delay(150);
                Assert(audio.IsPlaying, "Changing volume interrupted playback.");
                audio.Stop();
                Assert(!audio.IsPlaying, "STOP did not terminate playback.");
            }

            var temp = Path.Combine(Path.GetTempPath(), $"sovia-self-test-{Guid.NewGuid():N}");
            try
            {
                var settingsService = new SettingsService(temp);
                var settings = new AppSettings { Volume = 42, AlwaysOnTop = true };
                settings.SoundEdits["Effect 1/test.mp3"] = new SoundEdit { DisplayName = "TEST", CustomFilePath = "/tmp/test.mp3" };
                settingsService.Save(settings);
                var loaded = settingsService.Load();
                Assert(loaded.Volume == 42 && loaded.AlwaysOnTop, "Settings round-trip failed.");
                Assert(loaded.SoundEdits.TryGetValue("Effect 1/test.mp3", out var edit) && edit.DisplayName == "TEST", "EDIT settings round-trip failed.");
            }
            finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }

            Console.WriteLine("SELF_TEST_PASS: 120 audio files, playback/stop, live volume, always-on-top setting, and edit persistence.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SELF_TEST_FAIL: {ex}");
            return 1;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
