using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using AvaloniaCalculator.Engine;
using AvaloniaCalculator.Views;
using CalculationManager;
using AvaloniaCalculator.Localization;
using System.Globalization;

Locale.Use(new Locale("en-US"));

int passed=0;
void Check(string name,Action action) { action(); passed++; Console.WriteLine($"PASS {name}"); }
void Equal<T>(T expected,T actual) { if(!Equals(expected,actual)) throw new Exception($"Expected {expected}, got {actual}"); }
void Near(double expected,double actual) { if(Math.Abs(expected-actual)>1e-9*Math.Max(1,Math.Abs(expected))) throw new Exception($"Expected {expected}, got {actual}"); }
using(var engine=new CalculatorEngine())
{
    void Number(string value) { if(!engine.EnterNumber(value)) throw new Exception("Rejected number: "+value); }
    void Reset(CalculatorMode mode=CalculatorMode.Standard) { engine.SetMode(mode); engine.Send(Command.CommandCLEAR); }
    Check("12 + 34 = 46",()=> { Number("12");engine.Send(Command.CommandADD);Number("34");engine.Send(Command.CommandEQU);Equal("46",engine.State.Display); });
    Check("standard immediate evaluation",()=> { Reset();Number("2");engine.Send(Command.CommandADD);Number("3");engine.Send(Command.CommandMUL);Number("4");engine.Send(Command.CommandEQU);Equal("20",engine.State.Display); });
    Check("scientific precedence",()=> { Reset(CalculatorMode.Scientific);Number("2");engine.Send(Command.CommandADD);Number("3");engine.Send(Command.CommandMUL);Number("4");engine.Send(Command.CommandEQU);Equal("14",engine.State.Display); });
    Check("parentheses",()=> { Reset(CalculatorMode.Scientific);engine.Send(Command.CommandOPENP);Number("2");engine.Send(Command.CommandADD);Number("3");engine.Send(Command.CommandCLOSEP);engine.Send(Command.CommandMUL);Number("4");engine.Send(Command.CommandEQU);Equal("20",engine.State.Display); });
    Check("exact decimal arithmetic",()=> { Reset();Number("0.1");engine.Send(Command.CommandADD);Number("0.2");engine.Send(Command.CommandEQU);Equal("0.3",engine.State.Display); });
    Check("32 digit precision",()=> { Reset(CalculatorMode.Scientific);Number("999999999999999999999999999999");engine.Send(Command.CommandADD);Number("1");engine.Send(Command.CommandEQU);Equal("1000000000000000000000000000000",engine.State.Display.Replace(",","")); });
    Check("factorial",()=> { Reset(CalculatorMode.Scientific);Number("5");engine.Send(Command.CommandFAC);Equal("120",engine.State.Display); });
    Check("square root",()=> { Reset();Number("81");engine.Send(Command.CommandSQRT);Equal("9",engine.State.Display); });
    Check("trigonometry degrees",()=> { Reset(CalculatorMode.Scientific);engine.Send(Command.CommandDEG);Number("30");engine.Send(Command.CommandSIN);Equal("0.5",engine.State.Display); });
    Check("percentage",()=> { Reset();Number("200");engine.Send(Command.CommandADD);Number("10");engine.Send(Command.CommandPERCENT);engine.Send(Command.CommandEQU);Equal("220",engine.State.Display); });
    Check("divide by zero",()=> { Reset();Number("1");engine.Send(Command.CommandDIV);Number("0");engine.Send(Command.CommandEQU);Equal(true,engine.State.Error); });
    Check("recover from error",()=> { engine.Send(Command.CommandCLEAR);Number("7");Equal("7",engine.State.Display);Equal(false,engine.State.Error); });
    Check("memory store / add / recall",()=> { engine.Memory(5);Number("12");engine.Memory(0);Number("3");engine.Memory(2);engine.Memory(1);Equal("15",engine.State.Display); });
    Check("memory subtraction / clear",()=> { Number("5");engine.Memory(3);engine.Memory(1);Equal("10",engine.State.Display);engine.Memory(5);Equal(0,engine.State.Memory.Length); });
    Check("programmer hexadecimal",()=> { Reset(CalculatorMode.Programmer);Number("255");Equal("FF",engine.InBase(16));Equal("11111111",engine.InBase(2).Replace(" ","")); });
    Check("bitwise AND",()=> { Reset(CalculatorMode.Programmer);Number("12");engine.Send(Command.CommandAnd);Number("10");engine.Send(Command.CommandEQU);Equal("8",engine.State.Display); });
    Check("left shift",()=> { Reset(CalculatorMode.Programmer);Number("1");engine.Send(Command.CommandLSHF);Number("8");engine.Send(Command.CommandEQU);Equal("256",engine.State.Display); });
    Check("reject malformed input atomically",()=> { Reset();Number("42");Equal(false,engine.EnterNumber("1+2"));Equal("42",engine.State.Display);Equal(false,engine.EnterNumber("1.2.3")); });
    Check("copied grouped result round trips",()=> { Reset();Number("1000.25");var copied=engine.CopyText;Reset();Number(copied);Equal("1000.25",engine.CopyText); });
    Check("scientific exponent input",()=> { Reset(CalculatorMode.Scientific);Number("1.25e-3");engine.Send(Command.CommandEQU);Equal("0.00125",engine.CopyText); });
    Check("invalid exponent is atomic",()=> { Reset();Number("42");Equal(false,engine.EnterNumber("1e--2"));Equal("42",engine.CopyText); });
    Check("history is available and clears",()=> { Reset();Number("4");engine.Send(Command.CommandADD);Number("5");engine.Send(Command.CommandEQU);Equal("9",engine.State.History.Last().Result);engine.ClearHistory();Equal(0,engine.State.History.Length); });
}
Check("all 154 imported unit identities round trip",()=> { Equal(154,UnitCatalog.Units.Length);foreach(var u in UnitCatalog.Units) Near(42,UnitCatalog.Convert(42,u,u)); });
Check("all unit pairs round trip",()=> { foreach(var group in UnitCatalog.Units.GroupBy(x=>x.Category)) foreach(var a in group) foreach(var b in group) Near(123.45,UnitCatalog.Convert(UnitCatalog.Convert(123.45,a,b),b,a)); });
Check("Celsius to Fahrenheit",()=>Near(212,UnitCatalog.Convert(100,UnitCatalog.Units.Single(x=>x.Id=="Temperature_DegreesCelsius"),UnitCatalog.Units.Single(x=>x.Id=="Temperature_DegreesFahrenheit"))));
Check("mile to kilometres",()=>Near(1.609344,UnitCatalog.Convert(1,UnitCatalog.Units.Single(x=>x.Id=="Length_Mile"),UnitCatalog.Units.Single(x=>x.Id=="Length_Kilometer"))));
Check("leap day plus year",()=>Equal(new DateOnly(2025,2,28),DateMath.Add(new DateOnly(2024,2,29),1,0,0)));
Check("date difference",()=>Equal((1,1,2,395),DateMath.Difference(new DateOnly(2024,2,29),new DateOnly(2025,3,30))));
Check("reverse difference",()=>Equal((0,0,1,1),DateMath.Difference(new DateOnly(2026,11,24),new DateOnly(2026,11,23))));
Check("date subtraction order",()=>Equal(new DateOnly(2024,2,29),DateMath.Add(new DateOnly(2024,3,31),0,1,1,true)));

Check("ECB cross rates",()=> { var t=AvaloniaCalculator.Services.CurrencyRates.Parse("<Envelope><Cube><Cube time='2026-11-23'><Cube currency='USD' rate='1.1'/><Cube currency='GBP' rate='0.88'/></Cube></Cube></Envelope>"); Near(0.8,t.Rate("USD","GBP")); });
Check("reject invalid exchange rate",()=> { try { AvaloniaCalculator.Services.CurrencyRates.Parse("<Envelope><Cube time='2026-11-23'><Cube currency='USD' rate='NaN'/></Cube></Envelope>"); throw new Exception("Accepted invalid rate"); } catch(FormatException) {} });
Check("65 locale bundles and all unit resources",()=> {
    Equal(65,Locale.SupportedLanguages.Length);
    foreach(var language in Locale.SupportedLanguages)
    {
        var locale=new Locale(language);
        Equal(false,string.IsNullOrWhiteSpace(locale.Text("StandardModeText")));
        foreach(var unit in UnitCatalog.Units) { locale.Text("UnitName_"+unit.ResourceKey); locale.Text("UnitAbbreviation_"+unit.ResourceKey); }
        locale.Extra("ManualRate");locale.Extra("Cut");locale.EngineStrings();
    }
});
Check("POSIX session categories and language preferences",()=> {
    var env=new Dictionary<string,string>{{"LANG","en_US.UTF-8"},{"LANGUAGE","xx:it_IT:en_US"},{"LC_NUMERIC","de_DE.UTF-8"},{"LC_TIME","en_GB.UTF-8"}};
    var locale=Locale.FromEnvironment(key=>env.GetValueOrDefault(key));
    Equal("it-IT",locale.Language);Equal(",",locale.DecimalSeparator);Equal("23/11/2026",new DateOnly(2026,11,23).ToString("d",locale.Culture));
    env["LC_ALL"]="C";locale=Locale.FromEnvironment(key=>env.GetValueOrDefault(key));Equal("en-US",locale.Language);Equal(".",locale.DecimalSeparator);
    Equal("fr-FR",Locale.FromEnvironment(key=>env.GetValueOrDefault(key),"fr-FR").Language);
});
Check("locale fallback and script resolution",()=> {
    Equal("fr-FR",new Locale("fr_CH.UTF-8").Language);Equal("en-US",new Locale("xx-YY").Language);
    Equal("zh-TW",new Locale("zh-Hant-HK").Language);Equal("sr-Latn-RS",new Locale("sr_RS@latin").Language);
    Equal(true,new Locale("ar-SA").IsRightToLeft);
});
Check("localized number validation and Unicode digits",()=> {
    var it=new Locale("it-IT");
    Equal(true,LocalizedNumber.TryNormalize("1.234,56",it,out var value));Equal("1234.56",value);
    Equal(false,LocalizedNumber.TryNormalize("12.34,56",it,out _));
    Equal(true,LocalizedNumber.TryNormalize("1\u202f234,56",new Locale("fr-FR"),out value));Equal("1234.56",value);
    Equal(true,LocalizedNumber.TryNormalize("١٢٫٥",new Locale("ar-SA"),out value));Equal("12.5",value);
    Equal(true,LocalizedNumber.TryNormalize("12,34,567.89",new Locale("hi-IN"),out value));Equal("1234567.89",value);
    Equal(false,LocalizedNumber.TryNormalize("1,2",new Locale("en-US"),out _));
});
foreach(var language in new[]{"en-US","it-IT","de-DE","fr-FR","ar-SA","hi-IN","ja-JP"})
    Check("native localized arithmetic, precision, memory, history and errors: "+language,()=> {
        var locale=new Locale(language);using var engine=new CalculatorEngine(locale);
        string N(string text)=>text.Replace(".",locale.DecimalSeparator);
        Equal(true,engine.EnterNumber(N("0.1")));engine.Send(Command.CommandADD);Equal(true,engine.EnterNumber(N("0.2")));engine.Send(Command.CommandEQU);
        Equal(N("0.3"),engine.CopyText);Equal(N("0.3"),engine.State.History.Last().Result);
        engine.Memory(0);Equal(N("0.3"),engine.State.Memory[0]);
        engine.SetMode(CalculatorMode.Scientific);engine.EnterNumber("999999999999999999999999999999");engine.Send(Command.CommandADD);engine.EnterNumber("1");engine.Send(Command.CommandEQU);
        Equal("1000000000000000000000000000000",engine.CopyText);
        engine.Send(Command.CommandCLEAR);engine.EnterNumber(N("1234.5"));engine.Send(Command.CommandEQU);var grouped=engine.State.Display;
        Equal(true,engine.EnterNumber(grouped));Equal(N("1234.5"),engine.CopyText);
        engine.Send(Command.CommandCLEAR);engine.EnterNumber(N("1.25e-3"));engine.Send(Command.CommandEQU);Equal(N("0.00125"),engine.CopyText);
        engine.Send(Command.CommandCLEAR);engine.EnterNumber("1");engine.Send(Command.CommandDIV);engine.EnterNumber("0");engine.Send(Command.CommandEQU);
        Equal(true,engine.State.Error);Equal(locale.EngineText("99"),engine.State.Display);
    });
Check("programmer localized paste validates radix and hexadecimal E",()=> {
    using var engine=new CalculatorEngine(new Locale("it-IT"));engine.SetMode(CalculatorMode.Programmer);engine.Send((Command)313);
    Equal(true,engine.EnterNumber("FE"));Equal("FE",engine.CopyText);Equal(false,engine.EnterNumber("FG"));Equal("FE",engine.CopyText);
    engine.Send((Command)316);Equal(false,engine.EnterNumber("2"));Equal(true,engine.EnterNumber("1111 1110"));Equal("FE",engine.InBase(16));
});
AppBuilder.Configure<AvaloniaCalculator.App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
var window=new MainWindow(()=>Task.FromResult(new AvaloniaCalculator.Services.CurrencyTable(new DateOnly(2026,11,23),new Dictionary<string,double>{{"EUR",1},{"USD",1.0939}}))); window.Show(); Dispatcher.UIThread.RunJobs();
T Find<T>(string name) where T:Control => window.GetLogicalDescendants().OfType<T>().First(x=>x.Name==name);
void Click(string name) { Find<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
Check("Avalonia keypad wiring",()=> { Click("Key131");Click("Key132");Click("Key93");Click("Key133");Click("Key134");Click("Key121");Equal("46",Find<TextBlock>("Result").Text); });
Check("navigation to all 17 modes",()=> { for(int i=0;i<17;i++) { window.SelectMode(i);Dispatcher.UIThread.RunJobs(); } });
Check("ECB source survives deferred TextChanged",()=> {
    var manual=(Flyout)Find<Button>("ManualCurrency").Flyout!;
    var rate=((Control)manual.Content!).GetLogicalDescendants().OfType<TextBox>().Single(); Click("RefreshCurrency");Dispatcher.UIThread.RunJobs();Equal("1.0939",rate.Text);Equal("ECB · 11/23/2026",Find<TextBlock>("CurrencySource").Text);rate.Text="1.2";Dispatcher.UIThread.RunJobs();Equal("Manual rate",Find<TextBlock>("CurrencySource").Text); });
Check("converter UI updates on input",()=> { window.SelectMode(4);Find<TextBox>("ConversionInput").Text="2";Dispatcher.UIThread.RunJobs();Equal(false,string.IsNullOrEmpty(Find<TextBlock>("ConvertedResult").Text)); });
Check("phone width and landscape layout",()=> { window.Width=360;window.Height=740;window.UpdateLayout();Equal(true,window.Bounds.Width>=320);window.Width=740;window.Height=360;window.UpdateLayout(); });
Check("mobile standard follows Uno key placement",()=> {
    window.Width=408;window.Height=865;window.SelectMode(0);window.UpdateLayout();
    Equal(false,window.GetLogicalDescendants().OfType<Control>().Any(c=>c.Name=="NumberInput" || c.Name=="UseNumber"));
    var percent=Find<Button>("Key118");var root=Find<Button>("Key110");var ce=Find<Button>("Key82");
    Equal(0,Grid.GetRow(percent));Equal(0,Grid.GetColumn(percent));Equal(1,Grid.GetColumn(root));Equal(1,Grid.GetRow(ce));
    Equal(true,Find<Button>("Key121").Bounds.Height>48);
    Equal(Avalonia.Layout.HorizontalAlignment.Right,Find<Border>("ResultArea").Child!.HorizontalAlignment);
});
Check("mobile menu and history overlay",()=> {
    Click("OpenNavigation");Equal(true,Find<Grid>("NavigationOverlay").IsVisible);Click("CloseNavigation");
    Click("ShowHistory");Equal(true,Find<Grid>("HistoryOverlay").IsVisible);Click("ShowHistory");Equal(false,Find<Grid>("HistoryOverlay").IsVisible);
});
Check("scientific second function and hyperbolic controls",()=> {
    window.SelectMode(1);Click("Key81");Click("Key132");Click("Key111");Equal("4",Find<TextBlock>("Result").Text);
    Click("SecondFunction");Equal(true,Find<Button>("Key202").IsEnabled);Click("Hyperbolic");Equal(true,Find<Button>("Key206").IsEnabled);
});
Check("programmer radix and bit editor",()=> {
    window.SelectMode(2);Click("Key81");Click("Radix16");Click("Key145");Equal("F",Find<TextBlock>("Result").Text);
    Click("Radix2");Equal(false,Find<Button>("Key132").IsEnabled);Equal(true,Find<Button>("Key131").IsEnabled);
    Click("ShowBits");Click("Bit4");Equal("11111",window.Engine.InBase(2).Replace(" ",""));Click("ShowKeypad");
});
Check("converter uses its original numeric keypad",()=> {
    window.SelectMode(4);Click("ConversionKeyCE");Click("ConversionKey2");Click("ConversionKey5");Equal("25",Find<TextBox>("ConversionInput").Text);
    Equal(false,Find<Button>("ConversionKey±").IsVisible);window.SelectMode(8);Equal(true,Find<Button>("ConversionKey±").IsVisible);
});
Check("mode captions stay synchronized with native settings",()=> {
    window.SelectMode(1);Click("AngleUnit");window.SelectMode(0);window.SelectMode(1);
    Click("Key133");Click("Key130");Click("Key102");Equal("0.5",window.Engine.CopyText);
    window.SelectMode(2);Click("WordSize");Click("WordSize");Click("WordSize");
    window.SelectMode(0);window.SelectMode(2);window.Engine.EnterNumber("256");Equal("256",window.Engine.CopyText);
});
Check("wide history dock and narrow restoration",()=> {
    window.SelectMode(0);window.Width=800;window.Height=600;Dispatcher.UIThread.RunJobs();window.UpdateLayout();Equal(false,Find<Button>("ShowHistory").IsVisible);Equal(true,Find<Border>("DockHost").Child is not null);
    window.Width=408;window.Height=865;Dispatcher.UIThread.RunJobs();window.UpdateLayout();Equal(true,Find<Button>("ShowHistory").IsVisible);Equal(true,Find<Border>("DockHost").Child is null);
});
Check("hardware Unicode digits and directional marks",()=> {
    window.SelectMode(0);window.Engine.Send(Command.CommandCLEAR);
    foreach(var text in new[]{"١","٫","٥","\u200f"}) window.RaiseEvent(new Avalonia.Input.TextInputEventArgs { RoutedEvent=Avalonia.Input.InputElement.TextInputEvent,Text=text });
    Equal("1.5",window.Engine.CopyText);
});
window.Close();
foreach(var language in new[]{"it-IT","fr-FR","de-DE","ar-SA","ja-JP"})
    Check("localized mobile interface: "+language,()=> {
        Locale.Use(new Locale(language));
        var localized=new MainWindow(()=>Task.FromResult(new AvaloniaCalculator.Services.CurrencyTable(new DateOnly(2026,11,23),new Dictionary<string,double>{{"EUR",1},{"USD",1.0939}})));localized.Width=408;localized.Height=865;localized.Show();Dispatcher.UIThread.RunJobs();
        T Control<T>(string name) where T:Control=>localized.GetLogicalDescendants().OfType<T>().First(c=>c.Name==name);
        void Tap(string name) { Control<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs(); }
        Equal(Locale.Current.Text("StandardModeText"),Control<TextBlock>("ModeTitle").Text);
        Equal(Locale.Current.DecimalSeparator,Control<Button>("Key84").Content);
        Tap("Key131");Tap("Key84");Tap("Key135");Tap("Key93");Tap("Key132");Tap("Key121");
        Equal("3"+Locale.Current.DecimalSeparator+"5",localized.Engine.CopyText);
        Tap("ShowHistory");Control<ListBox>("History").SelectedIndex=0;Dispatcher.UIThread.RunJobs();Equal("3"+Locale.Current.DecimalSeparator+"5",localized.Engine.CopyText);
        for(int mode=0;mode<17;mode++) { localized.SelectMode(mode);Dispatcher.UIThread.RunJobs(); }
        localized.SelectMode(4);Tap("ConversionKeyCE");Tap("ConversionKey1");Tap("ConversionKey.");Tap("ConversionKey5");
        Equal("1"+Locale.Current.DecimalSeparator+"5",Control<TextBox>("ConversionInput").Text);
        Equal(Locale.Current.IsRightToLeft?Avalonia.Media.FlowDirection.RightToLeft:Avalonia.Media.FlowDirection.LeftToRight,localized.FlowDirection);
        localized.SelectMode(16);Tap("RefreshCurrency");
        var manual=(Flyout)Control<Button>("ManualCurrency").Flyout!;
        var rate=((Control)manual.Content!).GetLogicalDescendants().OfType<TextBox>().Single();
        Equal(1.0939.ToString("G15",Locale.Current.Culture),rate.Text);
        Equal("ECB · "+new DateOnly(2026,11,23).ToString("d",Locale.Current.Culture),Control<TextBlock>("CurrencySource").Text);
        rate.Text="2";Dispatcher.UIThread.RunJobs();Equal(Locale.Current.Extra("ManualRate"),Control<TextBlock>("CurrencySource").Text);
        localized.SelectMode(3);Control<DatePicker>("DateFrom").SelectedDate=new DateTimeOffset(2024,2,29,0,0,0,TimeSpan.Zero);
        Control<DatePicker>("DateTo").SelectedDate=new DateTimeOffset(2025,3,30,0,0,0,TimeSpan.Zero);Dispatcher.UIThread.RunJobs();
        Equal(Locale.Current.DateDifference(1,1,2,395),Control<TextBlock>("DateDifference").Text);
        Equal(new DateOnly(2024,2,29).ToString("d",Locale.Current.Culture),Control<TextBlock>("DateResult").Text);
        localized.Close();
    });
Locale.Use(new Locale("en-US"));
KeyboardLifecycleTests.Run(Check);
ThemeTests.Run(Check);
Console.WriteLine($"{passed} checks passed.");
