using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TiengCuoiSoviaMac;

public sealed class SoundItem : INotifyPropertyChanged
{
    private string _name = "";
    private string _filePath = "";
    private bool _isPlaying;

    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string OriginalName { get; init; }
    public required string OriginalFilePath { get; init; }
    public string Name { get => _name; set { _name = value; Changed(); } }
    public string FilePath { get => _filePath; set { _filePath = value; Changed(); } }
    public bool IsPlaying { get => _isPlaying; set { _isPlaying = value; Changed(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class AppSettings
{
    public double Volume { get; set; } = 75;
    public bool AlwaysOnTop { get; set; }
    public Dictionary<string, SoundEdit> SoundEdits { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SoundEdit
{
    public string DisplayName { get; set; } = "";
    public string? CustomFilePath { get; set; }
}
