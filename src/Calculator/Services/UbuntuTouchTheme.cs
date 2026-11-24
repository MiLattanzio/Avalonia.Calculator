using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AvaloniaCalculator.Services;

/// <summary>Follow the toolkit preference that Lomiri exposes to confined applications.</summary>
internal sealed class UbuntuTouchTheme : IDisposable
{
    private readonly Application _application;
    private readonly Window _window;
    private readonly Func<ThemeVariant?> _read;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    public UbuntuTouchTheme(Application application,Window window,Func<ThemeVariant?>? read=null)
    {
        _application=application; _window=window;
        _read=read ?? ReadSystemPreference;
        // The Click profile permits reading theme.ini, not enumerating/watching its
        // parent directory. XMir can leave input focus on PointerRoot even while
        // the app receives touch, so IsActive/Activated cannot gate this timer.
        // Poll while the window is visible; Lomiri manages process suspension.
        _timer=new DispatcherTimer { Interval=TimeSpan.FromSeconds(2) };
        _timer.Tick+=Tick;
        window.Activated+=Activated;
        window.PropertyChanged+=WindowPropertyChanged;
        window.Closed+=Closed;
        Refresh();
        if(window.IsVisible) _timer.Start();
    }
    internal void Refresh()
    {
        if(_disposed) return;
        // Preserve the last theme if an atomic settings write is temporarily unreadable.
        if(_read() is {} theme) _application.RequestedThemeVariant=theme;
    }
    internal static ThemeVariant? ReadSystemPreference() => ReadPreference(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    internal static ThemeVariant? ReadPreference(string home)
    {
        // Do not use the application's XDG data directory for a system preference.
        foreach(var toolkit in new[]{"lomiri-ui-toolkit","ubuntu-ui-toolkit","UITK"})
        {
            try { return Parse(File.ReadAllText(Path.Combine(home,".config",toolkit,"theme.ini"))) ?? ThemeVariant.Default; }
            catch(FileNotFoundException) { }
            catch(DirectoryNotFoundException) { }
            catch(IOException) { return null; }
            catch(UnauthorizedAccessException) { return null; }
        }
        return ThemeVariant.Default;
    }
    internal static ThemeVariant? Parse(string contents)
    {
        bool general=false;
        foreach(var raw in contents.TrimStart('\uFEFF').Split('\n'))
        {
            var line=raw.Trim();
            if(line.Length==0 || line[0] is '#' or ';') continue;
            if(line.StartsWith('[')) { general=line=="[General]";continue; }
            var equals=line.IndexOf('=');
            if(!general || equals<0 || line[..equals].Trim()!="theme") continue;
            var name=line[(equals+1)..].Trim().Trim('"').Split('.').Last();
            return name switch { "SuruDark"=>ThemeVariant.Dark,"Ambiance"=>ThemeVariant.Light,_=>null };
        }
        return null;
    }
    private void Tick(object? sender,EventArgs e)=>Refresh();
    private void Activated(object? sender,EventArgs e)=>Refresh();
    private void WindowPropertyChanged(object? sender,AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property!=Window.IsVisibleProperty) return;
        if(_window.IsVisible) { Refresh();_timer.Start(); }
        else _timer.Stop();
    }
    private void Closed(object? sender,EventArgs e)=>Dispose();
    public void Dispose()
    {
        if(_disposed) return;
        _disposed=true; _timer.Stop(); _timer.Tick-=Tick;
        _window.Activated-=Activated; _window.PropertyChanged-=WindowPropertyChanged; _window.Closed-=Closed;
    }
}
