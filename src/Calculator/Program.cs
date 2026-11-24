using System.Globalization;
using Avalonia;
using UTDotNet.Core;
using UTDotNet.Diagnostics;

namespace AvaloniaCalculator;

public static class Program
{
    public static string[] Arguments { get; private set; } = [];
    public static bool IsPhone => new PlatformDetector().Detect().MobileEnvironment == LinuxMobileEnvironment.UbuntuTouch;
    [STAThread]
    public static int Main(string[] args)
    {
        Arguments=args;
        Localization.Locale.Use(Localization.Locale.FromEnvironment(Environment.GetEnvironmentVariable, args.FirstOrDefault(a=>a.StartsWith("--locale="))?[9..]));
        ConfigureScale();
        try { return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); }
        catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder=AppBuilder.Configure<App>().UsePlatformDetect();
        if(IsPhone) builder=builder.With(new X11PlatformOptions { OverlayPopups=true });
        return builder;
    }
    private static void ConfigureScale()
    {
        if (!IsPhone || Environment.GetEnvironmentVariable("AVALONIA_SCREEN_SCALE_FACTORS") is not null ||
            Environment.GetEnvironmentVariable("AVALONIA_GLOBAL_SCALE_FACTOR") is not null) return;
        var grid=Environment.GetEnvironmentVariable("GRID_UNIT_PX");
        if(grid is null)
        {
            var result=new CommandRunner(TimeSpan.FromSeconds(2)).RunAsync("device-info", ["get","GridUnit"], CancellationToken.None).GetAwaiter().GetResult();
            if(result.Succeeded) grid=result.Output.Trim();
        }
        if(PhoneDisplayScale.FromGridUnit(grid,LinuxMobileEnvironment.UbuntuTouch) is {} scale)
            Environment.SetEnvironmentVariable("AVALONIA_SCREEN_SCALE_FACTORS",scale.ToString(CultureInfo.InvariantCulture));
    }
}
