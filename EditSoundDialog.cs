using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace TiengCuoiSoviaMac;

public sealed record EditResult(string Name, IStorageFile? SelectedFile, bool Restore);

public sealed class EditSoundDialog : Window
{
    private readonly TextBox _name;
    private readonly TextBlock _file;
    private IStorageFile? _selectedFile;

    public EditSoundDialog(string currentName, string currentFile, bool canRestore)
    {
        Title = "Chỉnh sửa âm thanh"; Width = 430; Height = 300; CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = new SolidColorBrush(Color.Parse("#080B18"));
        _name = new TextBox { Text = currentName, Height = 38, FontSize = 14, Foreground = Brushes.White, Background = new SolidColorBrush(Color.Parse("#111529")), BorderBrush = new SolidColorBrush(Color.Parse("#495778")) };
        _file = new TextBlock { Text = currentFile, Foreground = new SolidColorBrush(Color.Parse("#BEC9DC")), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        var browse = MakeButton("CHỌN MP3", "#25AABE"); browse.Click += Browse_Click;
        var restore = MakeButton("KHÔI PHỤC", "#2D3346"); restore.IsEnabled = canRestore; restore.Click += (_, _) => Close(new EditResult(currentName, null, true));
        var cancel = MakeButton("HỦY", "#2D3346"); cancel.Click += (_, _) => Close(null);
        var save = MakeButton("LƯU", "#FE2C95"); save.Click += (_, _) => Close(new EditResult(_name.Text ?? "", _selectedFile, false));

        var fileGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,105"), ColumnSpacing = 8 };
        fileGrid.Children.Add(new Border { Height = 38, Background = new SolidColorBrush(Color.Parse("#111529")), BorderBrush = new SolidColorBrush(Color.Parse("#495778")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 0), Child = _file });
        Grid.SetColumn(browse, 1); fileGrid.Children.Add(browse);
        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { cancel, save } };
        var actions = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { restore, right } }; Grid.SetColumn(right, 1);

        Content = new Grid
        {
            Margin = new Thickness(25, 20), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"), RowSpacing = 8,
            Children =
            {
                Place(new TextBlock { Text = "CHỈNH SỬA ÂM THANH", Foreground = new SolidColorBrush(Color.Parse("#25F4EE")), FontWeight = FontWeight.Bold, FontSize = 12 }, 0),
                Place(new StackPanel { Spacing = 5, Children = { Label("TÊN HIỂN THỊ"), _name } }, 1),
                Place(new StackPanel { Spacing = 5, Children = { Label("FILE ÂM THANH"), fileGrid } }, 2),
                Place(new TextBlock { Text = "File mới sẽ được sao chép vào dữ liệu của Sovia.", Foreground = new SolidColorBrush(Color.Parse("#7E8CA5")), FontSize = 10 }, 3),
                Place(actions, 4)
            }
        };
    }

    private async void Browse_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Chọn file âm thanh thay thế", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Tệp âm thanh") { Patterns = ["*.mp3", "*.wav", "*.m4a", "*.aac"] }]
        });
        if (files.Count == 0) return;
        _selectedFile = files[0];
        _file.Text = files[0].Name; _file.Foreground = new SolidColorBrush(Color.Parse("#25F4EE"));
    }

    private static T Place<T>(T control, int row) where T : Control { Grid.SetRow(control, row); return control; }
    private static TextBlock Label(string text) => new() { Text = text, Foreground = new SolidColorBrush(Color.Parse("#8B99B2")), FontSize = 9, FontWeight = FontWeight.Bold };
    private static Button MakeButton(string text, string color) => new() { Content = text, Height = 34, MinWidth = 72, Padding = new Thickness(12, 0), Foreground = Brushes.White, Background = new SolidColorBrush(Color.Parse(color)), BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(8), FontSize = 10, FontWeight = FontWeight.Bold };
}
