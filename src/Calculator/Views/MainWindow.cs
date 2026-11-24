using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaloniaCalculator.Engine;
using AvaloniaCalculator.Services;
using CalculationManager;
using AvaloniaCalculator.Localization;

namespace AvaloniaCalculator.Views;

public sealed partial class MainWindow : Window
{
    private static Locale L => Locale.Current;
    private static string T(string key) => L.Text(key);
    private static string A(string element) => L.Accessible(element);
    public CalculatorEngine Engine { get; } = new();
    public sealed record NavigationMode(int Id, string Title, string Glyph);
    private static readonly FontFamily CalculatorSymbols = new("avares://Avalonia.Calculator/Assets/CalcMDL2.ttf#Calculator MDL2 Assets");
    private readonly PhoneIntegration _phone;
    private readonly Func<Task<CurrencyTable>> _loadCurrency;
    private readonly List<Control> _pages = [];
    private NavigationMode[] _modes = [];
    private readonly Dictionary<int, Button> _navigationButtons = [];
    private int _page;
    private int _radix = 10;
    private bool _docked;
    private int _statusRevision;

    public MainWindow() : this(null) { }

    public MainWindow(Func<Task<CurrencyTable>>? loadCurrency) : this(loadCurrency,null) { }
    internal MainWindow(Func<Task<CurrencyTable>>? loadCurrency, UTDotNet.Avalonia.MaliitInputBridge? inputBridge)
    {
        InitializeComponent();
        Title=T("AppName");
        FlowDirection=L.IsRightToLeft?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
        AutomationProperties.SetName(OpenNavigation,A("FlyoutNav"));
        AutomationProperties.SetName(CloseNavigation,A("FlyoutNav"));
        AutomationProperties.SetName(ShowHistory,T("HistoryButton_Open"));
        _loadCurrency = loadCurrency ?? (() => CurrencyRates.DownloadAsync());
        _phone = new PhoneIntegration(this, ShellRoot,inputBridge);
        if (Program.IsPhone) WindowState = WindowState.Maximized;
        _modes = new[] {
            new NavigationMode(0,T("StandardModeText"),"\uE8EF"), new(1,T("ScientificModeText"),"\uF196"),
            new(2,T("ProgrammerModeText"),"\uECCE"), new(3,T("DateCalculationModeText"),"\uE787"),
            new(16,T("CategoryName_CurrencyText"),"\uEB0D"), new(6,T("CategoryName_VolumeText"),"\uF1AA"), new(4,T("CategoryName_LengthText"),"\uECC6"),
            new(7,T("CategoryName_WeightText"),"\uF4C1"), new(8,T("CategoryName_TemperatureText"),"\uE7A3"), new(9,T("CategoryName_EnergyText"),"\uECAD"),
            new(5,T("CategoryName_AreaText"),"\uE809"), new(11,T("CategoryName_SpeedText"),"\uEADA"), new(12,T("CategoryName_TimeText"),"\uE917"),
            new(10,T("CategoryName_PowerText"),"\uE945"), new(13,T("CategoryName_DataText"),"\uF20F"), new(14,T("CategoryName_PressureText"),"\uEC4A"), new(15,T("CategoryName_AngleText"),"\uF515") };
        foreach(var mode in _modes)
        {
            if(mode.Id is 0 or 16)
                Navigation.Children.Add(new TextBlock { Text=mode.Id==0?T("CalculatorModeText"):T("ConverterModeText"), FontWeight=FontWeight.SemiBold, Margin=new Thickness(12,16,0,8) });
            var row=new Grid { ColumnDefinitions=new ColumnDefinitions("36,*"),MinHeight=24 };
            row.Children.Add(new TextBlock { Text=mode.Glyph,FontFamily=CalculatorSymbols,FontSize=16,VerticalAlignment=VerticalAlignment.Center });
            Put(row,new TextBlock { Text=mode.Title,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center },0,1);
            var button=Button(mode.Title,()=>Navigate(mode.Id),"Navigate"+mode.Id);
            button.Classes.Add("navItem"); button.Content=row; button.Height=40; button.Focusable=true;
            button.HorizontalContentAlignment=HorizontalAlignment.Stretch;
            _navigationButtons[mode.Id]=button; Navigation.Children.Add(button);
        }
        OpenNavigation.Click += (_,_) => { _=_phone.HideAsync(); NavigationOverlay.IsVisible = !NavigationOverlay.IsVisible; };
        CloseNavigation.Click += (_,_) => NavigationOverlay.IsVisible = false;
        DismissNavigation.PointerPressed += (_,_) => NavigationOverlay.IsVisible = false;
        ShowHistory.Click += (_,_) => ToggleHistory(false);
        DismissHistory.PointerPressed += (_,_) => HistoryOverlay.IsVisible = false;
        _pages.Add(BuildCalculator()); _pages.Add(BuildDateCalculator()); _pages.Add(BuildConverter()); _pages.Add(BuildCurrency());
        foreach(var page in _pages) { page.IsVisible=false; PageHost.Children.Add(page); }
        BuildHistoryPane();
        Engine.Changed += Refresh;
        SelectMode(0);
        AddHandler(KeyDownEvent,OnKeyDown,Avalonia.Interactivity.RoutingStrategies.Bubble);
        TextInput += OnTextInput;
        SizeChanged += (_,_) => AdaptLayout();
        Opened += (_,_) =>
        {
            foreach(var arg in Program.Arguments)
            {
                if(arg.StartsWith("--width=") && double.TryParse(arg[8..],out var width)) Width=width;
                if(arg.StartsWith("--height=") && double.TryParse(arg[9..],out var height)) Height=height;
                if(arg.StartsWith("--mode=") && int.TryParse(arg[7..],out var mode)) SelectMode(mode);
            }
            AdaptLayout();
            if(Program.Arguments.Contains("--open-navigation")) NavigationOverlay.IsVisible=true;
            var screenshot=Program.Arguments.FirstOrDefault(a=>a.StartsWith("--screenshot=",StringComparison.Ordinal));
            if(screenshot is not null) DispatcherTimer.RunOnce(()=>
            {
                using var bitmap=new RenderTargetBitmap(PixelSize.FromSize(ClientSize,RenderScaling),new Vector(96*RenderScaling,96*RenderScaling));
                Console.WriteLine($"Screenshot: mode={_page}; result characters={_display.Text?.Length}; result bounds={_display.Bounds}; viewport={ClientSize}");
                bitmap.Render(this); bitmap.Save(screenshot[13..],PngBitmapEncoderOptions.Default);
                if(Program.Arguments.Contains("--exit-after-screenshot")) Close();
            },TimeSpan.FromSeconds(1));
        };
        Closed += (_,_) => { _phone.Dispose(); Engine.Dispose(); };
        Refresh();
    }

    public void SelectMode(int id) => Navigate(id);
    private void Navigate(int index)
    {
        _=_phone.HideAsync();
        _manualRateFlyout?.Hide();
        foreach(var page in _pages) page.IsVisible=false;
        _page=index; NavigationOverlay.IsVisible=false; HistoryOverlay.IsVisible=false;
        ModeTitle.Text=_modes.First(m=>m.Id==index).Title;
        foreach(var (id,button) in _navigationButtons) button.Classes.Set("selected",id==index);
        Focus();
        if(index<3)
        {
            _radix=10; _shifted=false; _hyperbolic=false; _angle=0; _word=0; _bitMode=false;
            Engine.SetMode((CalculatorMode)index);
            // The native engines retain angle/word settings across mode changes.
            // Keep them synchronized with the initial captions in the mobile controls.
            if(index==1) Engine.Send((Command)321);
            if(index==2) Engine.Send((Command)317);
            _pages[0].IsVisible=true; RebuildCalculator(); Refresh();
        }
        else if(index==3) _pages[1].IsVisible=true;
        else if(index<16)
        {
            _pages[2].IsVisible=true;
            if(_conversionNegate is not null) _conversionNegate.IsVisible=index==8;
            var category=UnitCatalog.Categories.Keys.ElementAt(index-4);
            var units=UnitCatalog.Units.Where(u=>u.Category==category).ToArray();
            _from.ItemsSource=units; _to.ItemsSource=units; _from.SelectedIndex=0; _to.SelectedIndex=Math.Min(1,units.Length-1); ConvertUnits();
        }
        else _pages[3].IsVisible=true;
        AdaptLayout();
    }
    private void AdaptLayout()
    {
        // MainPage.xaml DockVisible; narrow/mobile remains the overlay layout.
        _docked = ClientSize.Width>=560 && ClientSize.Height>=500 && _page<2;
        MainLayout.ColumnDefinitions[1].Width = new GridLength(_docked ? Math.Min(320,ClientSize.Width*.4) : 0);
        ShowHistory.IsVisible = _page<2 && !_docked;
        if(_historyPane is not null)
        {
            DockHost.Child=null; HistoryFlyoutHost.Child=null;
            if(_docked) { DockHost.Child=_historyPane; HistoryOverlay.IsVisible=false; }
            else HistoryFlyoutHost.Child=_historyPane;
            HistoryFlyoutHost.Height=Math.Min(Math.Max(200,ClientSize.Height*.63),Math.Max(200,ClientSize.Height-80));
        }
        _display.FontSize = _page==0 || ClientSize.Height>=(_page==1?544:640) ? 46 : 28;
        foreach(var resize in _converterLayouts) resize(ClientSize.Width>=640);
    }
    private void Status(string text)
    {
        StatusText.Text=text; StatusToast.IsVisible=!string.IsNullOrEmpty(text);
        var revision=++_statusRevision;
        DispatcherTimer.RunOnce(()=> { if(revision==_statusRevision) StatusToast.IsVisible=false; },TimeSpan.FromSeconds(4));
    }
    private static TextBlock Label(string text,int size=15) => new() { Text=text, FontSize=size, TextWrapping=TextWrapping.Wrap };
    private Button Button(string text,Action action,string? name=null)
    {
        var button=new Button { Content=text, Name=name, Focusable=false };
        button.Classes.Add("calc");
        string? element=name switch { "MemoryClear"=>"ClearMemoryButton","MemoryRecall"=>"MemRecall","MemoryAdd"=>"MemPlus","MemorySubtract"=>"MemMinus","MemoryStore"=>"memButton","Hyperbolic"=>"hyperbolicButton","ExponentFormat"=>"ftoeButton",_=>null };
        AutomationProperties.SetName(button,element is not null?A(element):name=="ShowMemory"?T("MemoryButton_Open"):text);
        button.Click+=(_,_)=> { try { action(); } catch(Exception) { Status(L.EngineText("100")); } };
        return button;
    }
    private Button AsyncButton(string text,Func<Task> action,string? name=null)
    {
        var button=Button(text,()=>{},name);
        button.Click+=async(_,_)=> { button.IsEnabled=false; try { await action(); } catch(Exception) { Status(L.EngineText("100")); } finally { button.IsEnabled=true; } };
        return button;
    }
    private static Grid Row(params Control[] controls)
    {
        var grid=new Grid { ColumnDefinitions=new ColumnDefinitions(string.Join(",",controls.Select(_=>"*"))) };
        for(int i=0;i<controls.Length;i++) { Grid.SetColumn(controls[i],i); grid.Children.Add(controls[i]); }
        return grid;
    }
    private static void Put(Grid grid,Control control,int row,int column=0,int columns=1)
    { Grid.SetRow(control,row); Grid.SetColumn(control,column); Grid.SetColumnSpan(control,columns); grid.Children.Add(control); }
    private static void Glyph(Button button,string glyph,int size=17,bool shell=false)
    { button.Content=new TextBlock { Text=glyph,FontFamily=CalculatorSymbols,FontSize=size }; }
    private async Task PasteResult()
    {
        var page=_page; var state=Engine.State;
        var text=await _phone.ReadAsync();
        if(page!=_page || !ReferenceEquals(state,Engine.State)) return;
        if(!string.IsNullOrWhiteSpace(text) && !Engine.EnterNumber(text.Trim())) Status(T("CannotPaste"));
    }
    private async void OnKeyDown(object? sender,KeyEventArgs e)
    {
        if(e.Key==Key.Escape && (NavigationOverlay.IsVisible || HistoryOverlay.IsVisible))
        { NavigationOverlay.IsVisible=false; HistoryOverlay.IsVisible=false; e.Handled=true; return; }
        if(_page>2 || e.Source is TextBox || FocusManager?.GetFocusedElement() is TextBox) return;
        if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if(e.Key==Key.C) { e.Handled=true; await _phone.CopyAsync(Engine.CopyText); return; }
            if(e.Key==Key.V) { e.Handled=true; await PasteResult(); return; }
        }
        Command? command=e.Key switch { Key.Enter=>Command.CommandEQU, Key.Back=>Command.CommandBACK, Key.Escape=>Command.CommandCLEAR, Key.Delete=>Command.CommandCENTR, _=>null };
        if(command.HasValue) { Engine.Send(command.Value); e.Handled=true; }
    }
    private void OnTextInput(object? sender,TextInputEventArgs e)
    {
        if(_page>2 || FocusManager?.GetFocusedElement() is TextBox || e.Text is not { Length:1 } text) return;
        text=LocalizedNumber.Digits(text);
        if(text.Length!=1) return;
        int digit="0123456789ABCDEF".IndexOf(char.ToUpperInvariant(text[0]));
        Command? command=digit>=0 && digit<(_page==2?_radix:10) ? (Command)(130+digit) : text[0] switch
        {
            '+'=>Command.CommandADD,'-'=>Command.CommandSUB,'*'=>Command.CommandMUL,'/'=>Command.CommandDIV,
            '='=>Command.CommandEQU,'.' or ',' or '٫' when _page!=2=>Command.CommandPNT,
            '(' when _page==1=>Command.CommandOPENP,')' when _page==1=>Command.CommandCLOSEP,
            '^' when _page==1=>Command.CommandPWR,'%' when _page!=2=>Command.CommandPERCENT,_=>null
        };
        if(command.HasValue) { Engine.Send(command.Value);e.Handled=true; }
    }
}
