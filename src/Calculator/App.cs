using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Styling;
using AvaloniaCalculator.Views;
using AvaloniaCalculator.Services;
using Avalonia.Markup.Xaml.Styling;

namespace AvaloniaCalculator;
public sealed class App : Application
{
    private UbuntuTouchTheme? _phoneTheme;
    // Optional diagnostic override for reproducible screenshots, not a saved preference.
    private static ThemeVariant? DiagnosticTheme => Program.Arguments.FirstOrDefault(a=>a.StartsWith("--theme="))?[8..] switch
    { "dark"=>ThemeVariant.Dark,"light"=>ThemeVariant.Light,_=>null };
    public override void Initialize()
    {
        RequestedThemeVariant=DiagnosticTheme ?? (Program.IsPhone?UbuntuTouchTheme.ReadSystemPreference():null) ?? ThemeVariant.Default;
        Styles.Add(new FluentTheme());
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Avalonia.Calculator/")) { Source=new Uri("avares://Avalonia.Calculator/Views/ThemeResources.axaml") });
        Resources["StringDatePickerDayText"]=Localization.Locale.Current.Text("Date_Day");
        Resources["StringDatePickerMonthText"]=Localization.Locale.Current.Text("Date_Month");
        Resources["StringDatePickerYearText"]=Localization.Locale.Current.Text("Date_Year");
        Resources["StringTextFlyoutCutText"]=Localization.Locale.Current.Extra("Cut");
        Resources["StringTextFlyoutCopyText"]=Localization.Locale.Current.Text("copyMenuItem");
        Resources["StringTextFlyoutPasteText"]=Localization.Locale.Current.Text("pasteMenuItem");
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow=new MainWindow();
            if(Program.IsPhone && DiagnosticTheme is null) _phoneTheme=new UbuntuTouchTheme(this,desktop.MainWindow);
            desktop.Exit+=(_,_)=>_phoneTheme?.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
