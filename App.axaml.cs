using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TiengCuoiSoviaMac;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;
            if (Program.UiSelfTestOutput is { } outputPath)
            {
                window.Opened += async (_, _) =>
                {
                    try
                    {
                        await window.RunUiSelfTestAsync(outputPath);
                        desktop.Shutdown(0);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"UI_SELF_TEST_FAIL: {ex}");
                        desktop.Shutdown(1);
                    }
                };
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
