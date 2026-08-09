using Avalonia;
using System;

namespace TiengCuoiSoviaMac;

internal static class Program
{
    internal static string? UiSelfTestOutput { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
            return SelfTest.RunAsync().GetAwaiter().GetResult();

        var uiSelfTestIndex = Array.FindIndex(args, value => value.Equals("--ui-self-test", StringComparison.OrdinalIgnoreCase));
        if (uiSelfTestIndex >= 0)
        {
            if (uiSelfTestIndex + 1 >= args.Length)
            {
                Console.Error.WriteLine("--ui-self-test requires an output PNG path.");
                return 2;
            }

            UiSelfTestOutput = Path.GetFullPath(args[uiSelfTestIndex + 1]);
            args = args.Where((_, index) => index != uiSelfTestIndex && index != uiSelfTestIndex + 1).ToArray();
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
