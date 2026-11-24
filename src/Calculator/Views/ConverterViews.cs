using System.Globalization;
using Avalonia.Automation;
using AvaloniaCalculator.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaCalculator.Engine;
using AvaloniaCalculator.Services;
using UTDotNet.Core;

namespace AvaloniaCalculator.Views;

public sealed partial class MainWindow
{
    private readonly TextBox _amount=new() { Name="ConversionInput",Text="0",MaxLength=100,IsReadOnly=true,Focusable=false };
    private readonly ComboBox _from=new() { Name="FromUnit" };
    private readonly ComboBox _to=new() { Name="ToUnit" };
    private readonly TextBlock _converted=new() { Name="ConvertedResult",FontSize=46,FontWeight=FontWeight.Light,VerticalAlignment=VerticalAlignment.Center };
    private readonly List<Action<bool>> _converterLayouts=[];
    private Button? _conversionNegate;
    private Flyout? _manualRateFlyout;
    private static bool Number(string? text,out double value) => LocalizedNumber.TryDouble(text,L,out value);
    private Control BuildConverter()
    {
        _amount.TextChanged+=(_,_)=>ConvertUnits(); _from.SelectionChanged+=(_,_)=>ConvertUnits(); _to.SelectionChanged+=(_,_)=>ConvertUnits();
        var supplementary=new TextBlock { FontSize=13,Margin=new Thickness(12,8),TextWrapping=TextWrapping.Wrap };
        return ConverterLayout(_amount,_from,_converted,_to,supplementary,"Conversion");
    }
    private void ConvertUnits()
    {
        if(_from.SelectedItem is not ConversionUnit from || _to.SelectedItem is not ConversionUnit to || from.Category!=to.Category) return;
        _converted.Text=Number(_amount.Text,out var value)?UnitCatalog.Convert(value,from,to).ToString("G12",L.Culture):"0";
    }
    private Grid ConverterLayout(TextBox amount,ComboBox from,TextBlock converted,ComboBox to,Control supplementary,string name)
    {
        // UnitConverter.xaml PortraitLayout: two readouts, two units, then 3-column numpad.
        var grid=new Grid { Name=name+"Layout",RowDefinitions=new RowDefinitions("56*,32*,56*,32*,Auto,272*"),ColumnDefinitions=new ColumnDefinitions("*,0") };
        amount.FlowDirection=converted.FlowDirection=FlowDirection.LeftToRight;
        amount.Classes.Add("readout"); amount.Margin=new Thickness(12,0); amount.VerticalAlignment=VerticalAlignment.Center;
        from.Classes.Add("unit"); to.Classes.Add("unit"); from.Margin=to.Margin=new Thickness(12,0); from.VerticalAlignment=to.VerticalAlignment=VerticalAlignment.Top;
        var result=new Viewbox { Child=converted,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(12,0),VerticalAlignment=VerticalAlignment.Center };
        Put(grid,amount,0); Put(grid,from,1); Put(grid,result,2); Put(grid,to,3); Put(grid,supplementary,4);
        var pad=new Grid { Name=name+"Keypad",FlowDirection=FlowDirection.LeftToRight,RowDefinitions=new RowDefinitions("*,*,*,*,*"),ColumnDefinitions=new ColumnDefinitions("0.25*,*,*,*,0.25*"),Margin=new Thickness(0,0,0,3) };
        void Edit(string key)
        {
            string text=amount.Text??"0";
            amount.Text=key switch {
                "CE"=>"0", "Back"=>text.Length>1?text[..^1]:"0", "±"=>text.StartsWith('-')?text[1..]:"-"+text,
                "."=>text.Contains(L.DecimalSeparator,StringComparison.Ordinal)?text:text+L.DecimalSeparator, _=>text=="0"?key:text.Length<100?text+key:text };
        }
        var labels=new string?[,]{{null,"CE","Back"},{"7","8","9"},{"4","5","6"},{"1","2","3"},{"±","0","."}};
        for(int row=0;row<5;row++) for(int col=0;col<3;col++)
        {
            var label=labels[row,col]; if(label is null || (label=="±" && name=="Currency")) continue;
            var button=Button(label=="."?L.DecimalSeparator:label,()=>Edit(label),name+"Key"+label); button.Margin=new Thickness(1);
            // UnitConverter.xaml binds this cell to CurrentCategory.NegateVisibility.
            if(label=="±") { _conversionNegate=button; button.IsVisible=false; }
            if(label.Length==1 && char.IsAsciiDigit(label[0])) { button.Classes.Add("numeric"); button.FontSize=34; }
            if(label=="Back") Glyph(button,"\uE94F"); if(label=="±") Glyph(button,"\uE94D");
            string? element=label switch { "CE"=>"clearEntryButton","Back"=>"backSpaceButton","±"=>"negateButton","."=>"decimalSeparatorButton",_=>null };
            if(element is not null) AutomationProperties.SetName(button,A(element));
            Put(pad,button,row,col+1);
        }
        Put(grid,pad,5,0,2);
        var copy=new MenuItem { Header=T("copyMenuItem"),MinHeight=48 }; copy.Click+=async(_,_)=>await _phone.CopyAsync(amount.Text??"0");
        var paste=new MenuItem { Header=T("pasteMenuItem"),MinHeight=48 }; paste.Click+=async(_,_)=> { var text=await _phone.ReadAsync(); if(Number(text,out _)) amount.Text=LocalizedNumber.TryNormalize(text,L,out var normalized)?normalized.Replace(".",L.DecimalSeparator):amount.Text; };
        amount.ContextMenu=new ContextMenu { ItemsSource=new[]{copy,paste} };
        _converterLayouts.Add(wide=>
        {
            grid.ColumnDefinitions[1].Width=new GridLength(wide?1:0,GridUnitType.Star);
            grid.RowDefinitions=new RowDefinitions(wide?"4*,2*,4*,2*,2*,0":"56*,32*,56*,32*,Auto,272*");
            foreach(int row in new[]{0,2}) grid.RowDefinitions[row].MinHeight=56;
            foreach(int row in new[]{1,3}) grid.RowDefinitions[row].MinHeight=32;
            grid.RowDefinitions[4].MinHeight=48;
            Grid.SetColumn(pad,wide?1:0); Grid.SetColumnSpan(pad,wide?1:2); Grid.SetRow(pad,wide?0:5); Grid.SetRowSpan(pad,wide?5:1);
            grid.Margin=new Thickness(wide?24:0,0);
        });
        return grid;
    }
    private Control BuildCurrency()
    {
        var from=new ComboBox { ItemsSource=new[]{"EUR","USD","GBP","CHF","JPY","CAD","AUD"},SelectedIndex=0 };
        var to=new ComboBox { ItemsSource=new[]{"EUR","USD","GBP","CHF","JPY","CAD","AUD"},SelectedIndex=1 };
        var amount=new TextBox { Name="CurrencyInput",Text="1",MaxLength=100,IsReadOnly=true,Focusable=false };
        var rate=new TextBox { Name="CurrencyRate",PlaceholderText=L.Extra("ManualRate"),FlowDirection=FlowDirection.LeftToRight,MinHeight=40,MaxLength=100 };
        _phone.Register(rate,"currency-rate",new InputRequest { InputType=InputType.Numeric });
        var result=new TextBlock { Text="—",FontSize=46,FontWeight=FontWeight.Light };
        var source=Label(T("RefreshButtonText.Content"),12); source.Name="CurrencySource";
        var ratio=Label("",12); ratio.FlowDirection=FlowDirection.LeftToRight;
        var ratePrompt=Label("",16); ratePrompt.FlowDirection=FlowDirection.LeftToRight;
        CurrencyTable? table=null; string? downloadedRateText=null;
        void Update()
        {
            bool validAmount=Number(amount.Text,out var a), validRate=Number(rate.Text,out var r);
            result.Text=validAmount&&validRate&&r>0&&double.IsFinite(a*r)?(a*r).ToString("G12",L.Culture):"—";
            ratio.Text=Number(rate.Text,out var rateValue)?L.Format("CurrencyFromToRatioFormat",1,from.SelectedItem!,rateValue.ToString("G12",L.Culture),to.SelectedItem!):"";
            ratePrompt.Text=$"1 {from.SelectedItem} = … {to.SelectedItem}";
        }
        void ApplyRate()
        {
            if(table is null || from.SelectedItem is not string f || to.SelectedItem is not string t) return;
            downloadedRateText=table.Rate(f,t).ToString("G15",L.Culture); rate.Text=downloadedRateText;
            source.Text=$"ECB · {table.Date.ToString("d",L.Culture)}";
        }
        amount.TextChanged+=(_,_)=>Update();
        rate.TextChanged+=(_,_)=> { Update(); source.Text=table is not null && rate.Text==downloadedRateText?$"ECB · {table.Date.ToString("d",L.Culture)}":L.Extra("ManualRate"); };
        from.SelectionChanged+=(_,_)=> { ApplyRate(); Update(); }; to.SelectionChanged+=(_,_)=> { ApplyRate(); Update(); };
        var update=AsyncButton(T("RefreshButtonText.Content"),async()=>
        {
            try
            {
                var fetched=await _loadCurrency(); var f=from.SelectedItem as string; var t=to.SelectedItem as string; table=fetched;
                var codes=fetched.Rates.Keys.Order().ToArray(); from.ItemsSource=codes; to.ItemsSource=codes;
                from.SelectedItem=codes.Contains(f)?f:"EUR"; to.SelectedItem=codes.Contains(t)?t:"USD"; ApplyRate(); Update();
            }
            catch(Exception) { source.Text=T("FailedToRefresh"); }
        },"RefreshCurrency");
        update.Classes.Add("caption"); update.Classes.Add("link"); update.HorizontalAlignment=HorizontalAlignment.Left; update.Height=32;
        var manual=Button(L.Extra("ManualRate"),()=>{},"ManualCurrency"); manual.Classes.Add("caption"); manual.Height=32; manual.HorizontalAlignment=HorizontalAlignment.Right;
        _manualRateFlyout=new Flyout { Content=new StackPanel { Spacing=8,Width=250,Children={ratePrompt,rate} } };
        // Closing runs before the popup detaches its editor (tap outside, Escape,
        // or navigation). Release input and restore the layout synchronously.
        _manualRateFlyout.Closing+=(_,e)=> { if(!e.Cancel) _=_phone.HideAsync(); };
        manual.Flyout=_manualRateFlyout;
        var details=new StackPanel { Margin=new Thickness(12,0,12,4),Spacing=3,Children={ratio,source,Row(update,manual)} };
        Update();
        return ConverterLayout(amount,from,result,to,details,"Currency");
    }
    private Control BuildDateCalculator()
    {
        // DateCalculator.xaml uses a mode selector, not two simultaneous forms.
        var stack=new StackPanel { Spacing=16,Margin=new Thickness(12,8),MaxWidth=440,HorizontalAlignment=HorizontalAlignment.Left };
        var mode=new ComboBox { Name="DateMode",ItemsSource=new[]{T("Date_DifferenceOption.Content"),T("Date_AddSubtractOption.Content")},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Left }; mode.Classes.Add("unit"); stack.Children.Add(mode);
        var start=new DatePicker { Name="DateFrom", SelectedDate=DateTimeOffset.Now,YearFormat="yyyy",HorizontalAlignment=HorizontalAlignment.Stretch };
        var end=new DatePicker { Name="DateTo", SelectedDate=DateTimeOffset.Now,YearFormat="yyyy",HorizontalAlignment=HorizontalAlignment.Stretch };
        var result=Label("",24); result.Name="DateDifference"; result.TextWrapping=TextWrapping.Wrap; var difference=new StackPanel { Spacing=12,Children={Label(T("Date_ToLabel.Text")),end,Label(T("Date_DifferenceLabel.Text")),result} };
        var addPanel=new StackPanel { Spacing=12,IsVisible=false };
        stack.Children.Add(Label(T("Date_FromLabel.Text"))); stack.Children.Add(start); stack.Children.Add(difference); stack.Children.Add(addPanel);
        void Difference()
        {
            if(start.SelectedDate is {} a && end.SelectedDate is {} b)
            { var d=DateMath.Difference(DateOnly.FromDateTime(a.DateTime),DateOnly.FromDateTime(b.DateTime)); result.Text=L.DateDifference(d.Years,d.Months,d.Days,d.TotalDays); }
        }
        var operation=new ComboBox { ItemsSource=new[]{T("AddOption.Content"),T("SubtractOption.Content")},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Left }; operation.Classes.Add("unit");
        var years=new TextBox { Name="DateYears",Text="0",MinHeight=40,MaxLength=4 }; var months=new TextBox { Name="DateMonths",Text="0",MinHeight=40,MaxLength=4 }; var days=new TextBox { Name="DateDays",Text="0",MinHeight=40,MaxLength=6 };
        foreach(var (field,id) in new[]{(years,"years"),(months,"months"),(days,"days")}) _phone.Register(field,id,new InputRequest { InputType=InputType.Numeric });
        var future=Label("",24); future.Name="DateResult"; future.TextWrapping=TextWrapping.Wrap; addPanel.Children.Add(operation); addPanel.Children.Add(Row(Label(T("YearsLabel.Text")),Label(T("MonthsLabel.Text")),Label(T("DaysLabel.Text")))); addPanel.Children.Add(Row(years,months,days)); addPanel.Children.Add(Label(T("DateLabel.Text"))); addPanel.Children.Add(future);
        void Add()
        {
            try
            {
                if(start.SelectedDate is {} date && LocalizedNumber.TryInt(years.Text,out var y)&&LocalizedNumber.TryInt(months.Text,out var m)&&LocalizedNumber.TryInt(days.Text,out var d)&&y>=0&&m>=0&&d>=0)
                    future.Text=DateMath.Add(DateOnly.FromDateTime(date.DateTime),y,m,d,operation.SelectedIndex==1).ToString("d",L.Culture);
                else future.Text=L.EngineText("100");
            }
            catch(ArgumentOutOfRangeException) { future.Text=T("Date_OutOfBoundMessage"); }
        }
        mode.SelectionChanged+=(_,_)=> { _=_phone.HideAsync(); difference.IsVisible=mode.SelectedIndex==0; addPanel.IsVisible=!difference.IsVisible; };
        start.SelectedDateChanged+=(_,_)=> { Difference(); Add(); }; end.SelectedDateChanged+=(_,_)=>Difference();
        operation.SelectionChanged+=(_,_)=>Add(); years.TextChanged+=(_,_)=>Add(); months.TextChanged+=(_,_)=>Add(); days.TextChanged+=(_,_)=>Add(); Difference(); Add();
        return new ScrollViewer { Content=stack,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }
}
