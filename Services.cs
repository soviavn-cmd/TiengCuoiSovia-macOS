using Avalonia.Threading;
using System.Diagnostics;
using System.Text.Json;

namespace TiengCuoiSoviaMac;

public sealed class AudioService : IDisposable
{
    private Process? _player;
    private double _volume = 75;
    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 100);
            var player = _player;
            if (player is null) return;
            try
            {
                if (!player.HasExited)
                {
                    player.StandardInput.WriteLine($"volume {_volume / 100d:0.000}".Replace(',', '.'));
                    player.StandardInput.Flush();
                }
            }
            catch { }
        }
    }
    public bool IsPlaying
    {
        get { try { return _player is { HasExited: false }; } catch { return false; } }
    }
    public event EventHandler? PlaybackFinished;

    public void Play(string path)
    {
        Stop();
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Bản này sử dụng bộ phát âm thanh dành cho macOS.");

        var helper = Path.Combine(AppContext.BaseDirectory, "SoviaAudioPlayer");
        if (!File.Exists(helper)) throw new FileNotFoundException("Thiếu bộ phát âm thanh SoviaAudioPlayer.", helper);
        var info = new ProcessStartInfo(helper) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true };
        info.ArgumentList.Add(path);
        info.ArgumentList.Add((_volume / 100d).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
        var player = new Process { StartInfo = info, EnableRaisingEvents = true };
        player.Exited += (_, _) =>
        {
            if (!ReferenceEquals(Interlocked.CompareExchange(ref _player, null, player), player)) return;
            player.Dispose();
            Dispatcher.UIThread.Post(() => PlaybackFinished?.Invoke(this, EventArgs.Empty));
        };
        _player = player;
        try
        {
            if (!player.Start()) throw new InvalidOperationException("Không thể khởi động bộ phát âm thanh.");
        }
        catch
        {
            Interlocked.CompareExchange(ref _player, null, player);
            player.Dispose();
            throw;
        }
    }

    public void Stop()
    {
        var player = Interlocked.Exchange(ref _player, null);
        if (player is null) return;
        try
        {
            if (!player.HasExited)
            {
                player.StandardInput.WriteLine("stop");
                player.StandardInput.Flush();
                if (!player.WaitForExit(1000)) player.Kill(true);
            }
        }
        catch { try { if (!player.HasExited) player.Kill(true); } catch { } }
        finally { player.Dispose(); }
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
        var temporary = $"{FilePath}.tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, _json));
        File.Move(temporary, FilePath, true);
    }
}
