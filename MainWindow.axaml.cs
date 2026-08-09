using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace TiengCuoiSoviaMac;

public partial class MainWindow : Window
{
    private static readonly string[] Categories = ["Effect 1", "Effect 2", "Music 1", "Music 2"];
    private readonly ObservableCollection<SoundItem> _visible = [];
    private readonly List<SoundItem> _all = [];
    private readonly AudioService _audio = new();
    private readonly SettingsService _settingsService = new();
    private readonly AppSettings _settings;
    private SoundItem? _current;
    private string _category = "Effect 1";
    private bool _editMode;
    private bool _volumeDragging;

    public MainWindow()
    {
        InitializeComponent();
        Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://TiengCuoiSovia/Assets/Sovia.ico")));
        _settings = _settingsService.Load();
        SoundItems.ItemsSource = _visible;
        VolumeHost.SizeChanged += (_, _) => UpdateVolumeVisual(_settings.Volume);
        AlwaysOnTopToggle.IsChecked = _settings.AlwaysOnTop;
        AlwaysOnTopToggle.IsCheckedChanged += AlwaysOnTop_Changed;
        EditToggle.IsCheckedChanged += EditToggle_Changed;
        Topmost = _settings.AlwaysOnTop;
        _audio.Volume = _settings.Volume;
        _audio.PlaybackFinished += (_, _) => ResetPlayer();
        Opened += (_, _) => LoadSounds();
        Closing += (_, _) => { _settingsService.Save(_settings); _audio.Dispose(); };
    }

    private void LoadSounds()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Media");
        if (!Directory.Exists(root)) { StatusText.Text = "Không tìm thấy thư mục Media."; return; }
        foreach (var category in Categories)
        {
            var folder = Path.Combine(root, category);
            if (!Directory.Exists(folder)) continue;
            foreach (var path in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
                         .Where(p => new[] { ".mp3", ".wav", ".m4a", ".aac" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
                         .OrderBy(NaturalKey))
            {
                var id = Path.GetRelativePath(root, path).Replace('\\', '/');
                var name = Regex.Replace(Path.GetFileNameWithoutExtension(path), @"^\d+\s+", "").Trim().ToUpperInvariant();
                var sound = new SoundItem { Id = id, Category = category, Name = name, FilePath = path, OriginalName = name, OriginalFilePath = path };
                if (_settings.SoundEdits.TryGetValue(id, out var edit))
                {
                    if (!string.IsNullOrWhiteSpace(edit.DisplayName)) sound.Name = edit.DisplayName;
                    if (!string.IsNullOrWhiteSpace(edit.CustomFilePath) && File.Exists(edit.CustomFilePath)) sound.FilePath = edit.CustomFilePath;
                }
                _all.Add(sound);
            }
        }
        RefreshCategory();
    }

    private static string NaturalKey(string path) => Regex.Replace(Path.GetFileName(path), @"^\d+", m => m.Value.PadLeft(5, '0'));

    private void RefreshCategory()
    {
        _visible.Clear();
        foreach (var item in _all.Where(x => x.Category == _category)) _visible.Add(item);
        StatusText.Text = $"{_category}: {_visible.Count} hiệu ứng.";
    }

    private void Category_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button chosen || chosen.Tag is not string category) return;
        SelectCategory(category, chosen);
    }

    private void SelectCategory(string category, Button chosen)
    {
        _category = category;
        foreach (var button in new[] { Effect1Button, Effect2Button, Music1Button, Music2Button })
            button.Background = new SolidColorBrush(button == chosen ? Color.Parse("#FE2C95") : Color.Parse("#111529"));
        RefreshCategory();
    }

    private async void SoundButton_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not SoundItem sound) return;
        if (_editMode) await EditSoundAsync(sound); else PlaySound(sound);
    }

    private void PlaySound(SoundItem sound)
    {
        if (_current == sound && _audio.IsPlaying) { Stop(); return; }
        try
        {
            if (_current is not null) _current.IsPlaying = false;
            _audio.Play(sound.FilePath);
            _current = sound;
            sound.IsPlaying = true;
            StatusText.Text = $"Đang phát: {sound.Name}";
        }
        catch (Exception ex) { ResetPlayer(); ShowError(ex.Message); }
    }

    private async Task EditSoundAsync(SoundItem sound)
    {
        if (_current == sound) Stop();
        var dialog = new EditSoundDialog(sound.Name, Path.GetFileName(sound.FilePath), sound.Name != sound.OriginalName || sound.FilePath != sound.OriginalFilePath);
        var result = await dialog.ShowDialog<EditResult?>(this);
        if (result is null) return;
        if (result.Restore)
        {
            if (_settings.SoundEdits.TryGetValue(sound.Id, out var previous)) DeleteCustomFile(previous.CustomFilePath);
            sound.Name = sound.OriginalName; sound.FilePath = sound.OriginalFilePath; _settings.SoundEdits.Remove(sound.Id);
            StatusText.Text = $"Đã khôi phục: {sound.Name}";
        }
        else
        {
            var name = string.IsNullOrWhiteSpace(result.Name) ? sound.OriginalName : result.Name.Trim().ToUpperInvariant();
            var custom = _settings.SoundEdits.TryGetValue(sound.Id, out var old) ? old.CustomFilePath : null;
            if (result.SelectedFile is not null)
            {
                Directory.CreateDirectory(_settingsService.CustomMediaFolder);
                DeleteCustomFile(custom);
                custom = Path.Combine(_settingsService.CustomMediaFolder, $"{Guid.NewGuid():N}{Path.GetExtension(result.SelectedFile.Name)}");
                await using var input = await result.SelectedFile.OpenReadAsync();
                await using var output = File.Create(custom);
                await input.CopyToAsync(output);
                sound.FilePath = custom;
            }
            sound.Name = name; _settings.SoundEdits[sound.Id] = new SoundEdit { DisplayName = name, CustomFilePath = custom };
            StatusText.Text = $"Đã cập nhật: {sound.Name}";
        }
        _settingsService.Save(_settings);
    }

    private void DeleteCustomFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var root = Path.GetFullPath(_settingsService.CustomMediaFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(path);
            if (candidate.StartsWith(root, StringComparison.Ordinal) && File.Exists(candidate)) File.Delete(candidate);
        }
        catch { }
    }

    private async void ShowError(string message)
    {
        var dialog = new Window { Title = "Tiếng Cười Sovia", Width = 380, Height = 150, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = new SolidColorBrush(Color.Parse("#080B18")) };
        var ok = new Button { Content = "OK", Width = 72, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
        ok.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel { Margin = new Thickness(20), Spacing = 18, Children = { new TextBlock { Text = message, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap }, ok } };
        await dialog.ShowDialog(this);
    }

    private void Stop() { _audio.Stop(); ResetPlayer(); }
    private void ResetPlayer() { if (_current is not null) _current.IsPlaying = false; _current = null; StatusText.Text = $"Đã tải {_all.Count} hiệu ứng từ Sovia."; }
    private void Stop_Click(object? sender, RoutedEventArgs e) => Stop();
    private void EditToggle_Changed(object? sender, RoutedEventArgs e) { _editMode = EditToggle.IsChecked == true; StatusText.Text = _editMode ? "EDIT: chọn nút để đổi tên hoặc file MP3." : $"Đã tải {_all.Count} hiệu ứng từ Sovia."; }
    private void AlwaysOnTop_Changed(object? sender, RoutedEventArgs e) { Topmost = AlwaysOnTopToggle.IsChecked == true; _settings.AlwaysOnTop = Topmost; }
    private void Volume_PointerPressed(object? sender, PointerPressedEventArgs e) { _volumeDragging = true; e.Pointer.Capture(VolumeHost); SetVolumeFromPointer(e); }
    private void Volume_PointerMoved(object? sender, PointerEventArgs e) { if (_volumeDragging) SetVolumeFromPointer(e); }
    private void Volume_PointerReleased(object? sender, PointerReleasedEventArgs e) { if (!_volumeDragging) return; SetVolumeFromPointer(e); _volumeDragging = false; e.Pointer.Capture(null); _settingsService.Save(_settings); }
    private void SetVolumeFromPointer(PointerEventArgs e)
    {
        var width = Math.Max(1, VolumeHost.Bounds.Width);
        var value = Math.Clamp(e.GetPosition(VolumeHost).X / width * 100d, 0, 100);
        SetVolume(value);
    }

    private void SetVolume(double value)
    {
        value = Math.Clamp(value, 0, 100);
        _settings.Volume = value;
        _audio.Volume = value;
        UpdateVolumeVisual(value);
    }
    private void UpdateVolumeVisual(double value)
    {
        var width = Math.Max(1, VolumeHost.Bounds.Width);
        var x = Math.Clamp(width * value / 100d, 0, width);
        VolumeFill.Width = x;
        Canvas.SetLeft(VolumeThumb, Math.Clamp(x - 9.5, 0, Math.Max(0, width - 19)));
        Canvas.SetLeft(VolumeThumbCenter, Math.Clamp(x - 3.5, 6, Math.Max(6, width - 13)));
        VolumeText.Text = $"{value:0}";
    }
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e) { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); }
    private void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    internal async Task RunUiSelfTestAsync(string outputPath)
    {
        await Task.Delay(350);

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        Require(Math.Abs(Bounds.Width - 342) < 0.5, $"Window width is {Bounds.Width}, expected 342.");
        Require(Math.Abs(Bounds.Height - 680) < 0.5, $"Window height is {Bounds.Height}, expected 680.");
        Require(_all.Count == 120, $"Loaded {_all.Count} sounds, expected 120.");

        var buttons = new Dictionary<string, Button>
        {
            ["Effect 1"] = Effect1Button,
            ["Effect 2"] = Effect2Button,
            ["Music 1"] = Music1Button,
            ["Music 2"] = Music2Button
        };
        foreach (var (category, button) in buttons)
        {
            SelectCategory(category, button);
            Require(_visible.Count == 30, $"{category} contains {_visible.Count} buttons, expected 30.");
            Require(_visible.All(item => item.Category == category), $"{category} shows an item from another category.");
        }

        var originalTopmost = Topmost;
        var originalVolume = _settings.Volume;
        AlwaysOnTopToggle.IsChecked = true;
        Require(Topmost && _settings.AlwaysOnTop, "Always-on-top did not turn on.");
        AlwaysOnTopToggle.IsChecked = false;
        Require(!Topmost && !_settings.AlwaysOnTop, "Always-on-top did not turn off.");

        EditToggle.IsChecked = true;
        Require(_editMode, "Edit mode did not turn on.");
        EditToggle.IsChecked = false;
        Require(!_editMode, "Edit mode did not turn off.");

        SetVolume(42);
        Require(Math.Abs(_audio.Volume - 42) < 0.01, "Audio service volume was not updated.");
        Require(VolumeText.Text == "42", $"Volume label is '{VolumeText.Text}', expected '42'.");
        Require(VolumeFill.Width > 0 && VolumeFill.Width < VolumeHost.Bounds.Width, "Volume fill is outside the track.");

        SelectCategory("Effect 1", Effect1Button);
        await Task.Delay(150);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        using (var bitmap = new RenderTargetBitmap(new PixelSize(342, 680), new Vector(96, 96)))
        {
            bitmap.Render(this);
            bitmap.Save(outputPath, PngBitmapEncoderOptions.Default);
        }
        Require(File.Exists(outputPath) && new FileInfo(outputPath).Length > 0, "UI screenshot was not created.");

        AlwaysOnTopToggle.IsChecked = originalTopmost;
        SetVolume(originalVolume);
        _settingsService.Save(_settings);
        Console.WriteLine($"UI_SELF_TEST_PASS: 120 sounds, 4 categories, toggles, volume, layout, screenshot={outputPath}");
    }
}
