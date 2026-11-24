using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaCalculator.Engine;
using CalculationManager;

namespace AvaloniaCalculator.Views;

public sealed partial class MainWindow
{
    private readonly TextBlock _display = new() { Name="Result",Text="0",FontSize=46,FontWeight=FontWeight.SemiBold,TextAlignment=TextAlignment.Right };
    private readonly TextBlock _expression = new() { Name="Expression",Classes={"secondary"},TextAlignment=TextAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(6,0),TextTrimming=TextTrimming.CharacterEllipsis };
    private readonly Grid _calculator = new() { Name="CalculatorLayout",FlowDirection=FlowDirection.LeftToRight };
    private readonly Grid _modeControls = new();
    private readonly Grid _memoryControls = new();
    private readonly Border _keypadHost = new();
    private readonly List<(int Command,Button Button)> _keys = [];
    private readonly List<(int Radix,Button Button,TextBlock Text)> _baseRows = [];
    private readonly List<(int Bit,Button Button)> _bits = [];
    private Button? _mc, _mr, _memoryToggle;
    private bool _shifted, _hyperbolic, _bitMode;
    private int _angle, _word;
    private readonly ListBox _history = new() { Name="History" };
    private readonly ListBox _memory = new() { Name="Memory" };
    private Grid? _historyPane;
    private readonly TextBlock _emptyHistory = new() { TextWrapping=TextWrapping.Wrap, Text=T("HistoryEmpty.Text"),Margin=new Thickness(16,18),FontWeight=FontWeight.SemiBold };
    private readonly TextBlock _emptyMemory = new() { Text=T("MemoryPaneEmpty.Text"),Margin=new Thickness(16,18),TextWrapping=TextWrapping.Wrap };
    private Button? _historyTab, _memoryTab;
    private bool _showMemory;

    private Control BuildCalculator()
    {
        Put(_calculator,_expression,0);
        // Styles.xaml CalculationResultStyleM uses a 75 DIP Viewbox, top/right, with 12 DIP gutters.
        var result = new Border { Name="ResultArea",Background=Brushes.Transparent,Margin=new Thickness(12,0),
            Child=new Viewbox { Child=_display,Height=75,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top } };
        var copy=new MenuItem { Header=T("copyMenuItem"),Name="CopyResult",MinHeight=48 };
        var paste=new MenuItem { Header=T("pasteMenuItem"),Name="PasteResult",MinHeight=48 };
        copy.Click+=async(_,_)=>await _phone.CopyAsync(Engine.CopyText);
        paste.Click+=async(_,_)=>await PasteResult();
        var menu=new ContextMenu { ItemsSource=new[]{copy,paste} };
        result.ContextMenu=menu;
        result.SetValue(InputElement.IsHoldingEnabledProperty,true);
        result.Holding+=(_,e)=> { if(e.HoldingState==HoldingState.Started) { menu.Open(result);e.Handled=true; } };
        Put(_calculator,result,1); Put(_calculator,_modeControls,2); Put(_calculator,_memoryControls,3); Put(_calculator,_keypadHost,4);
        return _calculator;
    }
    private void RebuildCalculator()
    {
        // Calculator.xaml mobile rows, after the shared 40 DIP navigation row.
        _calculator.RowDefinitions=new RowDefinitions(_page switch { 1=>"20*,72*,32*,32*,276*",2=>"20*,72*,96*,32*,268*",_=>"20*,72*,0,32*,308*" });
        _calculator.RowDefinitions[0].MinHeight=20; _calculator.RowDefinitions[1].MinHeight=72;
        _calculator.RowDefinitions[2].MinHeight=_page==0?0:_page==1?32:96;
        _calculator.RowDefinitions[3].MinHeight=32;
        _modeControls.Children.Clear(); _memoryControls.Children.Clear(); _baseRows.Clear();
        if(_page==1)
        {
            var angles=Button(new[]{"DEG","RAD","GRAD"}[_angle],()=> { _angle=(_angle+1)%3; Engine.Send((Command)(321+_angle)); RebuildCalculator(); },"AngleUnit");
            AutomationProperties.SetName(angles,A(new[]{"degButton","radButton","gradButton"}[_angle]));
            var hyp=Button("HYP",()=> { _hyperbolic=!_hyperbolic; RebuildCalculator(); },"Hyperbolic");
            if(_hyperbolic) hyp.Classes.Add("selected");
            var fe=Button("F-E",()=>Engine.Send(Command.CommandFE),"ExponentFormat");
            var row=Row(angles,hyp,fe,new Border(),new Border(),new Border());
            foreach(var button in new[]{angles,hyp,fe}) button.Classes.Add("caption");
            _modeControls.Children.Add(row);
        }
        if(_page==2)
        {
            var bases=new Grid { RowDefinitions=new RowDefinitions("*,*,*,*"),Margin=new Thickness(3,0) };
            for(int i=0;i<4;i++)
            {
                var index=i; var radix=new[]{16,10,8,2}[i];
                var button=Button(new[]{"HEX","DEC","OCT","BIN"}[i],()=> { _radix=radix; Engine.Send((Command)(313+index)); },"Radix"+radix); button.Classes.Add("caption");
                var line=new Grid { ColumnDefinitions=new ColumnDefinitions("58,*"),Margin=new Thickness(12,0),HorizontalAlignment=HorizontalAlignment.Stretch };
                line.Children.Add(new TextBlock { Text=new[]{"HEX","DEC","OCT","BIN"}[i],FontSize=15,VerticalAlignment=VerticalAlignment.Center });
                var value=new TextBlock { FontSize=15,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis }; Put(line,value,0,1);
                button.Content=line; button.HorizontalContentAlignment=HorizontalAlignment.Stretch;
                _baseRows.Add((radix,button,value)); Put(bases,button,i);
            }
            _modeControls.Children.Add(bases);
            var keypad=Button(A("fullKeypad"),()=> { _bitMode=false; RebuildCalculator(); },"ShowKeypad"); Glyph(keypad,"\uE75F",20);
            var bits=Button(A("bitFlip"),()=> { _bitMode=true; RebuildCalculator(); },"ShowBits"); Glyph(bits,"\uF7D0",20);
            (_bitMode?bits:keypad).Classes.Add("selected");
            var word=Button(new[]{"QWORD","DWORD","WORD","BYTE"}[_word],()=> { _word=(_word+1)%4; Engine.Send((Command)(317+_word)); RebuildCalculator(); },"WordSize");
            AutomationProperties.SetName(word,A(new[]{"qwordButton","dwordButton","wordButton","byteButton"}[_word]));
            _memoryToggle=Button("M⌄",()=>ToggleHistory(true),"ShowMemory");
            var store=Button("MS",()=>Engine.Memory(0),"MemoryStore");
            foreach(var b in new[]{keypad,bits,word,store,_memoryToggle}) b.Classes.Add("caption");
            _memoryControls.Children.Add(Row(keypad,bits,word,new Border(),store,_memoryToggle));
            _mc=null; _mr=null;
        }
        else
        {
            _mc=Button("MC",()=>Engine.Memory(5),"MemoryClear"); _mr=Button("MR",RecallMemory,"MemoryRecall");
            var add=Button("M+",()=>ChangeMemory(2),"MemoryAdd"); var subtract=Button("M−",()=>ChangeMemory(3),"MemorySubtract");
            var store=Button("MS",()=>Engine.Memory(0),"MemoryStore"); _memoryToggle=Button("M⌄",()=>ToggleHistory(true),"ShowMemory");
            var row=Row(_mc,_mr,add,subtract,store,_memoryToggle); row.Margin=new Thickness(3,0);
            foreach(var button in row.Children.OfType<Button>()) button.Classes.Add("caption");
            _memoryControls.Children.Add(row);
        }
        BuildKeypad(); Refresh();
    }
    private Button KeyButton(string text,int command)
    {
        var button=Button(command==84?L.DecimalSeparator:text,()=>Engine.Send((Command)command),"Key"+command); button.Margin=new Thickness(1);
        if(command is >=130 and <=145) button.Classes.Add("numeric");
        var glyph=command switch {
            118=>"\uE94C",110=>"\uE94B",111=>"\uF7C8",114=>"\uF7C9",83=>"\uE94F",
            91=>"\uE94A",92=>"\uE947",94=>"\uE949",93=>"\uE948",80=>"\uE94D",121=>"\uE94E",
            97=>"\uF7CA",112=>"\uF7CB",96=>"\uF7CD",117=>"\uF7CC",205=>"\uF7CE",120=>"\uF7CF",_=>null };
        if(glyph is not null) Glyph(button,glyph);
        if(command==84) { button.FontSize=24; button.FontWeight=FontWeight.Bold; }
        if(KeyElement(command) is {} element) AutomationProperties.SetName(button,A(element));
        _keys.Add((command,button)); return button;
    }
    private static string? KeyElement(int command) => command switch
    {
        80=>"negateButton",81=>"clearButton",82=>"clearEntryButton",83=>"backSpaceButton",84=>"decimalSeparatorButton",
        86=>"andButton",87=>"orButton",88=>"xorButton",89=>"lshButton",90=>"rshButton",91=>"divideButton",92=>"multiplyButton",
        93=>"plusButton",94=>"minusButton",95=>"modButton",96=>"ySquareRootButton",97=>"powerButton",99=>"rolButton",100=>"rorButton",101=>"notButton",
        102=>"sinButton",103=>"cosButton",104=>"tanButton",105=>"sinhButton",106=>"coshButton",107=>"tanhButton",
        108=>"logBaseEButton",109=>"logBase10Button",110=>"squareRootButton",111=>"xpower2Button",112=>"xpower3Button",113=>"factorialButton",
        114=>"invertButton",115=>"dmsButton",117=>"powerOf10Button",118=>"percentButton",120=>"piButton",121=>"equalButton",127=>"expButton",
        128=>"openParenthesisButton",129=>"closeParenthesisButton",202=>"invsinButton",203=>"invcosButton",204=>"invtanButton",
        205=>"powerOfEButton",206=>"invsinhButton",207=>"invcoshButton",208=>"invtanhButton",324=>"degreesButton",_=>null
    };
    private void BuildKeypad()
    {
        _keys.Clear(); _bits.Clear();
        var pad=new Grid { Name="Keypad",Margin=new Thickness(3,0,3,3) };
        if(_page==2 && _bitMode) { BuildBitPanel(pad); _keypadHost.Child=pad; return; }
        var rows=_page==1?7:6; var columns=_page==0?4:_page==1?5:6;
        pad.RowDefinitions=new RowDefinitions(string.Join(",",Enumerable.Repeat("*",rows)));
        pad.ColumnDefinitions=new ColumnDefinitions(string.Join(",",Enumerable.Repeat("*",columns)));
        (string Text,int Command)[][] keys;
        if(_page==0) keys=[
            [("%",118),("√",110),("x²",111),("1/x",114)],
            [("CE",82),("C",81),(A("backSpaceButton"),83),("÷",91)],
            [("7",137),("8",138),("9",139),("×",92)],
            [("4",134),("5",135),("6",136),("−",94)],
            [("1",131),("2",132),("3",133),("+",93)],
            [("±",80),("0",130),(".",84),("=",121)]];
        else if(_page==1)
        {
            int trig=_hyperbolic?(_shifted?206:105):(_shifted?202:102);
            string suffix=(_hyperbolic?"h":"")+(_shifted?"⁻¹":"");
            keys=[
                [(_shifted?"x³":"x²",_shifted?112:111),(_shifted?"ʸ√x":"xʸ",_shifted?96:97),("sin"+suffix,trig),("cos"+suffix,trig+1),("tan"+suffix,trig+2)],
                [(_shifted?"1/x":"√",_shifted?114:110),(_shifted?"eˣ":"10ˣ",_shifted?205:117),(_shifted?"ln":"log",_shifted?108:109),(_shifted?"dms":"Exp",_shifted?115:127),(_shifted?"deg":"Mod",_shifted?324:95)],
                [(A("shiftButton"),-1),("CE",82),("C",81),(A("backSpaceButton"),83),("÷",91)],
                [("π",120),("7",137),("8",138),("9",139),("×",92)],
                [("n!",113),("4",134),("5",135),("6",136),("−",94)],
                [("±",80),("1",131),("2",132),("3",133),("+",93)],
                [("(",128),(")",129),("0",130),(".",84),("=",121)]];
        }
        else keys=[
            [(_shifted?"RoL":"Lsh",_shifted?99:89),(_shifted?"RoR":"Rsh",_shifted?100:90),("Or",87),("Xor",88),("Not",101),("And",86)],
            [(A("shiftButton"),-1),("Mod",95),("CE",82),("C",81),(A("backSpaceButton"),83),("÷",91)],
            [("A",140),("B",141),("7",137),("8",138),("9",139),("×",92)],
            [("C",142),("D",143),("4",134),("5",135),("6",136),("−",94)],
            [("E",144),("F",145),("1",131),("2",132),("3",133),("+",93)],
            [("(",128),(")",129),("±",80),("0",130),(".",84),("=",121)]];
        for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
        {
            var key=keys[row][col]; Button button;
            if(key.Command==-1)
            {
                button=Button(key.Text,()=> { _shifted=!_shifted; BuildKeypad(); Refresh(); },"SecondFunction");
                Glyph(button,"\uE752",20); button.Margin=new Thickness(1); if(_shifted) button.Classes.Add("selected");
            }
            else button=KeyButton(key.Text,key.Command);
            Put(pad,button,row,col);
        }
        _keypadHost.Child=pad;
    }
    private void BuildBitPanel(Grid pad)
    {
        // CalculatorProgrammerBitFlipPanel.xaml: four nibbles per line, index below each nibble.
        pad.RowDefinitions=new RowDefinitions("*,*,*,*,*,*,*,*");
        pad.ColumnDefinitions=new ColumnDefinitions("0.8*,*,*,*,*,0.8*,*,*,*,*,0.8*,*,*,*,*,0.8*,*,*,*,*,0.8*");
        for(int bit=63;bit>=0;bit--)
        {
            int value=bit;
            var button=Button("0",()=>
            {
                if(!ulong.TryParse(Engine.InBase(16).Replace(" ",""),System.Globalization.NumberStyles.HexNumber,null,out var current)) return;
                var updated=(current^(1UL<<value)).ToString("X");
                Engine.Send((Command)313); Engine.Send(Command.CommandCENTR);
                foreach(char c in updated) Engine.Send((Command)(130+"0123456789ABCDEF".IndexOf(c)));
                Engine.Send((Command)(313+Array.IndexOf(new[]{16,10,8,2},_radix)));
            },"Bit"+bit);
            button.Classes.Add("caption"); button.FontSize=20; button.FontWeight=FontWeight.SemiBold;
            int row=(63-bit)/16*2, column=1+(63-bit)%16+((63-bit)%16)/4;
            _bits.Add((bit,button)); Put(pad,button,row,column);
            if(bit%4==0) Put(pad,new TextBlock { Text=bit.ToString(),FontSize=12,Classes={"secondary"},HorizontalAlignment=HorizontalAlignment.Center },row+1,column);
        }
    }
    private void RecallMemory() { if(Engine.State.Memory.Length>0) Engine.Memory(1,Math.Max(0,_memory.SelectedIndex)); }
    private void ChangeMemory(int action) { if(Engine.State.Memory.Length==0) Engine.Memory(0); else Engine.Memory(action,Math.Max(0,_memory.SelectedIndex)); }
    private void Refresh()
    {
        _display.Text=Engine.State.Display; _expression.Text=Engine.State.Expression;
        _display.FlowDirection=Engine.State.Error && L.IsRightToLeft?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
        _history.ItemsSource=Engine.State.History.Reverse().ToArray(); _memory.ItemsSource=Engine.State.Memory;
        _emptyHistory.IsVisible=Engine.State.History.Length==0 && !_showMemory;
        _emptyMemory.IsVisible=Engine.State.Memory.Length==0 && _showMemory;
        bool memory=Engine.State.Memory.Length>0;
        if(_mc is not null) _mc.IsEnabled=memory; if(_mr is not null) _mr.IsEnabled=memory;
        if(_memoryToggle is not null) _memoryToggle.IsEnabled=memory;
        foreach(var (command,button) in _keys)
            button.IsEnabled=_page!=2 || (command!=84 && (command<130 || command>145 || command-130<_radix));
        foreach(var (radix,button,text) in _baseRows)
        {
            text.Text=Engine.InBase(radix); button.BorderThickness=new Thickness(radix==_radix?3:0,0,0,0);
        }
        if(_bits.Count>0 && ulong.TryParse(Engine.InBase(16).Replace(" ",""),System.Globalization.NumberStyles.HexNumber,null,out var value))
            foreach(var (bit,button) in _bits)
            {
                bool set=((value>>bit)&1)!=0; button.Content=set?"1":"0";
                button.IsEnabled=bit<new[]{64,32,16,8}[_word]; button.Classes.Set("activeBit",set);
            }
    }
    private void BuildHistoryPane()
    {
        _historyPane=new Grid { Name="HistoryPane",RowDefinitions=new RowDefinitions("48,*,48") };
        _historyTab=Button(T("HistoryPane"),()=>SelectHistoryTab(false),"HistoryTab"); _memoryTab=Button(T("MemoryPane"),()=>SelectHistoryTab(true),"MemoryTab");
        foreach(var button in new[]{_historyTab,_memoryTab}) { button.Classes.Add("caption"); button.FontSize=15; button.FontWeight=FontWeight.SemiBold; button.Margin=new Thickness(8,0,8,4); }
        Put(_historyPane,Row(_historyTab,_memoryTab),0);
        var lists=new Grid(); lists.Children.Add(_history); lists.Children.Add(_memory); lists.Children.Add(_emptyHistory); lists.Children.Add(_emptyMemory); Put(_historyPane,lists,1);
        _history.ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<HistoryEntry>((h,_)=>
        {
            var stack=new StackPanel { FlowDirection=FlowDirection.LeftToRight,Spacing=6,HorizontalAlignment=HorizontalAlignment.Stretch };
            stack.Children.Add(new TextBlock { Text=h?.Expression,Classes={"secondary"},TextAlignment=TextAlignment.Right,TextWrapping=TextWrapping.Wrap });
            stack.Children.Add(new TextBlock { Text=h?.Result,FontSize=24,FontWeight=FontWeight.SemiBold,TextAlignment=TextAlignment.Right,TextWrapping=TextWrapping.Wrap }); return stack;
        });
        _history.SelectionChanged+=(_,_)=> { if(_history.SelectedItem is HistoryEntry h) { _history.SelectedIndex=-1; Engine.EnterNumber(h.Result); HistoryOverlay.IsVisible=false; } };
        _memory.ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<string>((value,_)=>
        {
            var stack=new StackPanel { FlowDirection=FlowDirection.LeftToRight,Spacing=8 };
            stack.Children.Add(new TextBlock { Text=value,FontSize=24,FontWeight=FontWeight.SemiBold,TextAlignment=TextAlignment.Right });
            var row=Row(Button("MC",()=>MemoryItem(value,4)),Button("M+",()=>MemoryItem(value,2)),Button("M−",()=>MemoryItem(value,3))); row.Height=36;
            foreach(var button in row.Children.OfType<Button>()) button.Classes.Add("caption"); stack.Children.Add(row); return stack;
        });
        _memory.SelectionChanged+=(_,_)=> { if(_memory.SelectedIndex>=0) { var i=_memory.SelectedIndex; _memory.SelectedIndex=-1; Engine.Memory(1,i); HistoryOverlay.IsVisible=false; } };
        var clear=Button(T("ClearHistory.Name"),()=> { if(_showMemory) Engine.Memory(5); else Engine.ClearHistory(); },"ClearList"); Glyph(clear,"\uE74D",16,true); clear.Classes.Add("caption"); clear.Width=48; clear.HorizontalAlignment=HorizontalAlignment.Right;
        Put(_historyPane,clear,2); SelectHistoryTab(false);
    }
    private void MemoryItem(string? value,int action)
    {
        var index=Array.IndexOf(Engine.State.Memory,value); if(index>=0) Engine.Memory(action,index);
    }
    private void SelectHistoryTab(bool memory)
    {
        if(this.FindControl<Button>("ClearList") is {} clear) AutomationProperties.SetName(clear,memory?A("ClearMemory"):T("ClearHistory.Name"));
        _showMemory=memory; _history.IsVisible=!memory; _memory.IsVisible=memory;
        _historyTab?.Classes.Set("selected",!memory); _memoryTab?.Classes.Set("selected",memory); Refresh();
    }
    private void ToggleHistory(bool memory)
    {
        bool close=HistoryOverlay.IsVisible && _showMemory==memory;
        SelectHistoryTab(memory); if(!_docked) HistoryOverlay.IsVisible=!close;
    }
}
