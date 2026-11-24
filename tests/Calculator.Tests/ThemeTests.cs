using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaCalculator.Services;
using AvaloniaCalculator.Views;
using CalculationManager;

internal static class ThemeTests
{
    public static void Run(Action<string,Action> check)
    {
        var app=Application.Current!;var original=app.RequestedThemeVariant;
        try
        {
            check("theme: parse Lomiri and legacy toolkit settings",()=> {
                Assert(UbuntuTouchTheme.Parse("[General]\ntheme=Lomiri.Components.Themes.SuruDark\n")==ThemeVariant.Dark,"SuruDark not recognized");
                Assert(UbuntuTouchTheme.Parse("\uFEFF[General]\r\n theme = \"Ubuntu.Components.Themes.SuruDark\" \r\n")==ThemeVariant.Dark,"legacy dark preference not recognized");
                Assert(UbuntuTouchTheme.Parse("; comment\n[General]\ntheme=Lomiri.Components.Themes.Ambiance")==ThemeVariant.Light,"Ambiance not recognized");
                Assert(UbuntuTouchTheme.Parse("[Keyboard]\ntheme=SuruDark\n[General]\ntheme=Ambiance")==ThemeVariant.Light,"keyboard theme affected application theme");
                Assert(UbuntuTouchTheme.Parse("[General]\n# theme=SuruDark\ntheme=Custom") is null,"unknown theme incorrectly classified");
            });
            check("theme: prefer current toolkit and fall back to legacy or platform",()=> {
                var home=Directory.CreateTempSubdirectory("calculator-theme-");
                try
                {
                    Assert(UbuntuTouchTheme.ReadPreference(home.FullName)==ThemeVariant.Default,"missing setting should follow platform");
                    var legacy=Write(home.FullName,"ubuntu-ui-toolkit","SuruDark");
                    Assert(UbuntuTouchTheme.ReadPreference(home.FullName)==ThemeVariant.Dark,"legacy theme ignored");
                    var current=Write(home.FullName,"lomiri-ui-toolkit","Ambiance");
                    Assert(UbuntuTouchTheme.ReadPreference(home.FullName)==ThemeVariant.Light,"legacy preference overrode current toolkit");
                    File.Delete(current);Assert(UbuntuTouchTheme.ReadPreference(home.FullName)==ThemeVariant.Dark,"removed current setting did not fall back");
                    File.Delete(legacy);Assert(UbuntuTouchTheme.ReadPreference(home.FullName)==ThemeVariant.Default,"removed settings did not restore platform default");
                }
                finally { home.Delete(true); }
            });
            check("theme: foreground app follows settings edits without reopening",()=> {
                var home=Directory.CreateTempSubdirectory("calculator-theme-live-");
                var window=new MainWindow();
                try
                {
                    Write(home.FullName,"lomiri-ui-toolkit","SuruDark");window.Show();window.Activate();Pump();
                    Assert(window.IsActive,"test window was not activated");
                    using var theme=new UbuntuTouchTheme(app,window,()=>UbuntuTouchTheme.ReadPreference(home.FullName));
                    Assert(window.ActualThemeVariant==ThemeVariant.Dark,"startup preference not applied");
                    Write(home.FullName,"lomiri-ui-toolkit","Ambiance");
                    Until(()=>window.ActualThemeVariant==ThemeVariant.Light);
                    Write(home.FullName,"lomiri-ui-toolkit","SuruDark");
                    Until(()=>window.ActualThemeVariant==ThemeVariant.Dark);
                }
                finally { window.Close();home.Delete(true); }
            });
            check("theme: refresh on resume, retain theme on read failure, detach on close",()=> {
                ThemeVariant? preference=ThemeVariant.Dark;var reads=0;
                var window=new MainWindow();var other=new Window();
                using var theme=new UbuntuTouchTheme(app,window,()=> { reads++;return preference; });
                try
                {
                    window.Show();window.Activate();Pump();
                    preference=null;theme.Refresh();Assert(window.ActualThemeVariant==ThemeVariant.Dark,"read failure flashed light theme");
                    other.Show();other.Activate();Pump();preference=ThemeVariant.Light;
                    window.Activate();Pump();Assert(window.ActualThemeVariant==ThemeVariant.Light,"resume did not refresh preference");
                    window.Close();var count=reads;theme.Refresh();Assert(reads==count,"closed window kept reading theme");
                }
                finally { other.Close();window.Close(); }
            });
            check("theme: visible XMir window follows changes without activation",()=> {
                ThemeVariant? preference=ThemeVariant.Dark;var reads=0;
                var window=new Window { ShowActivated=false };
                using var theme=new UbuntuTouchTheme(app,window,()=> { reads++;return preference; });
                try
                {
                    window.Show();Pump();Assert(window.IsVisible && !window.IsActive,"expected visible, inactive window");
                    preference=ThemeVariant.Light;Until(()=>window.ActualThemeVariant==ThemeVariant.Light);
                    window.Hide();Pump();var count=reads;var until=DateTime.UtcNow.AddMilliseconds(2200);
                    while(DateTime.UtcNow<until) { Pump();Thread.Sleep(5); }
                    Assert(reads==count,"hidden window kept polling");
                    preference=ThemeVariant.Dark;window.Show();Pump();
                    Assert(window.ActualThemeVariant==ThemeVariant.Dark,"reshown window did not refresh");
                }
                finally { window.Close(); }
            });
            check("theme: live palette changes preserve calculation, memory and history",()=> {
                var window=new MainWindow();
                try
                {
                    app.RequestedThemeVariant=ThemeVariant.Light;window.Show();Pump();
                    var result=Find<TextBlock>(window,"Result");var numeric=Find<Button>(window,"Key131");
                    var operation=Find<Button>(window,"Key93");var disabled=Find<Button>(window,"MemoryRecall");
                    var bounds=numeric.Bounds;
                    ColorIs(window.Background,"#FFE6E6E6");ColorIs(result.Foreground,"#FF000000");ColorIs(disabled.Foreground,"#66000000");
                    window.Engine.EnterNumber("1.25");window.Engine.Memory(0);window.Engine.Send(Command.CommandADD);window.Engine.EnterNumber("2");window.Engine.Send(Command.CommandEQU);
                    var state=window.Engine.State;
                    app.RequestedThemeVariant=ThemeVariant.Dark;Pump();
                    ColorIs(window.Background,"#FF1F1F1F");ColorIs(result.Foreground,"#FFFFFFFF");
                    ColorIs(numeric.Background,"#CC000000");ColorIs(operation.Background,"#66000000");ColorIs(numeric.Foreground,"#FFFFFFFF");
                    ColorIs(((TextBlock)operation.Content!).Foreground,"#FFFFFFFF");
                    foreach(var path in window.GetLogicalDescendants().OfType<Avalonia.Controls.Shapes.Path>()) ColorIs(path.Stroke,"#FFFFFFFF");
                    Assert(ReferenceEquals(state,window.Engine.State),"theme change mutated calculation state");
                    Assert(result.Text=="3.25" && numeric.Bounds==bounds,"theme change altered value or layout");
                    app.RequestedThemeVariant=ThemeVariant.Light;Pump();ColorIs(numeric.Background,"#CCFFFFFF");ColorIs(result.Foreground,"#FF000000");
                }
                finally { window.Close(); }
            });
            check("theme: all modes and programmer bit states use the active palette",()=> {
                var window=new MainWindow();
                try
                {
                    window.Show();Pump();
                    foreach(var theme in new[]{ThemeVariant.Dark,ThemeVariant.Light})
                    {
                        app.RequestedThemeVariant=theme;var foreground=theme==ThemeVariant.Dark?"#FFFFFFFF":"#FF000000";
                        for(int mode=0;mode<17;mode++) { window.SelectMode(mode);Pump();ColorIs(Find<TextBlock>(window,"ModeTitle").Foreground,foreground); }
                        window.SelectMode(2);Click(window,"ShowBits");Click(window,"Bit0");
                        ColorIs(Find<Button>(window,"Bit0").Foreground,theme==ThemeVariant.Dark?"#FF60CDFF":"#FF0078D7");
                        Click(window,"WordSize");Click(window,"WordSize");Click(window,"WordSize");
                        ColorIs(Find<Button>(window,"Bit40").Foreground,theme==ThemeVariant.Dark?"#66FFFFFF":"#66000000");
                        window.SelectMode(4);ColorIs(Find<TextBox>(window,"ConversionInput").Foreground,foreground);
                        ColorIs(Find<TextBlock>(window,"ConvertedResult").Foreground,foreground);
                    }
                }
                finally { window.Close(); }
            });
            check("theme: open rate popup and clipboard menu follow changes without losing input",()=> {
                var window=new MainWindow();
                try
                {
                    app.RequestedThemeVariant=ThemeVariant.Light;window.Show();window.SelectMode(16);Pump();
                    var button=Find<Button>(window,"ManualCurrency");var flyout=(Flyout)button.Flyout!;
                    flyout.ShowAt(button);Pump();var rate=((Control)flyout.Content!).GetLogicalDescendants().OfType<TextBox>().Single();
                    rate.Text="1.25";rate.Focus();Pump();
                    app.RequestedThemeVariant=ThemeVariant.Dark;Pump();
                    Assert(flyout.IsOpen && rate.IsFocused && rate.Text=="1.25","theme change disturbed active rate editor");
                    Assert(rate.ActualThemeVariant==ThemeVariant.Dark,"popup kept light theme");ColorIs(rate.Foreground,"#FFFFFFFF");
                    rate.SelectAll();var menu=rate.ContextFlyout!;menu.ShowAt(rate);Pump();
                    app.RequestedThemeVariant=ThemeVariant.Light;Pump();
                    Assert(rate.ActualThemeVariant==ThemeVariant.Light && rate.Text=="1.25","clipboard theme change lost input");
                    menu.Hide();flyout.Hide();Pump();
                }
                finally { window.Close(); }
            });
        }
        finally { app.RequestedThemeVariant=original;Pump(); }
    }
    private static string Write(string home,string toolkit,string theme)
    {
        var folder=System.IO.Path.Combine(home,".config",toolkit);Directory.CreateDirectory(folder);
        var path=System.IO.Path.Combine(folder,"theme.ini");File.WriteAllText(path,"[General]\ntheme=Lomiri.Components.Themes."+theme+"\n");return path;
    }
    private static T Find<T>(Window window,string name) where T:Control=>window.GetLogicalDescendants().OfType<T>().First(c=>c.Name==name);
    private static void Click(Window window,string name) { Find<Button>(window,name).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));Pump(); }
    private static void ColorIs(IBrush? brush,string expected) => Assert(brush is ISolidColorBrush color && color.Color==Color.Parse(expected),$"Expected brush {expected}, got {brush}");
    private static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
    private static void Pump()
    {
        // A timer-only headless queue needs a dispatcher turn to promote due timers.
        Dispatcher.UIThread.Post(()=>{});
        Dispatcher.UIThread.RunJobs();
    }
    private static void Until(Func<bool> condition)
    {
        var timeout=DateTime.UtcNow.AddSeconds(4);
        while(!condition() && DateTime.UtcNow<timeout) { Pump();Thread.Sleep(5); }
        Pump();Assert(condition(),"timed out waiting for system theme change");
    }
}
