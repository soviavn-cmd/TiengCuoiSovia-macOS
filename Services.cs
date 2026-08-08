using Avalonia.Threading;
using System.Diagnostics;
using System.Text.Json;

namespace TiengCuoiSoviaMac;

public sealed class AudioService : IDisposable
{
    private Process? _player;
    public double Volume { get; set; } = 75;
    public bool IsPlaying => _player is { HasExited: false };
    public event EventHandler? PlaybackFinished;

    public void Play(string path)
    {
        Stop();
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Bản này phát âm thanh bằng afplay của macOS.");

        var info = new ProcessStartInfo("/usr/bin/afplay") { UseShellExecute = false, CreateNoWindow = true };
        info.ArgumentList.Add("-v");
        info.ArgumentList.Add(Math.Clamp(Volume / 100d, 0, 1).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        info.ArgumentList.Add(path);
        _player = new Process { StartInfo = info, EnableRaisingEvents = true };
        _player.Exited += (_, _) => Dispatcher.UIThread.Post(() => PlaybackFinished?.Invoke(this, EventArgs.Empty));
        _player.Start();
    }

    public void Stop()
    {
        if (_player is null) return;
        try { if (!_player.HasExited) _player.Kill(true); } catch { }
        _player.Dispose();
        _player = null;
    }

    public void Dispose() => Stop();
}

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    public string DataFolder { get; }
    public string CustomMediaFolder => Path.Combine(DataFolder, "CustomMedia");
    private string FilePath => Path.Combine(DataFolder, "settings.json");

    public SettingsService(string? dataFolder = null) => DataFolder = dataFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TiengCuoiSovia");

    public AppSettings Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), _json) ?? new() : new(); }
        catch { return new(); }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(CustomMediaFolder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, _json));
    }
}
