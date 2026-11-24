using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using UTDotNet.Avalonia;
using UTDotNet.Core;
using UTDotNet.Diagnostics;
using UTDotNet.UbuntuTouch;

namespace AvaloniaCalculator.Services;
public sealed class PhoneIntegration : IDisposable
{
    private readonly Window _window;
    private readonly Border _root;
    private readonly DiagnosticLog _log=new();
    private readonly MaliitInputBridge? _bridge;
    private readonly ITextClipboard? _clipboard;
    private readonly List<IDisposable> _registrations=[];
    private readonly List<TextBox> _fields=[];
    private readonly Thickness _originalPadding;
    private InputArea _area;
    private TextBox? _focused;
    private bool _disposed;
    public PhoneIntegration(Window window, Border root) : this(window,root,null) { }
    internal PhoneIntegration(Window window, Border root, MaliitInputBridge? inputBridge)
    {
        _window=window; _root=root;
        _originalPadding=root.Padding;
        _bridge=inputBridge ?? (Program.IsPhone?new MaliitInputBridge(_log):null);
        if(Program.IsPhone) _clipboard=new ContentHubClipboard(_log);
        if(_bridge is not null)
        {
            _bridge.InputAreaChanged+=AreaChanged;
            window.SizeChanged+=SizeChanged;
            window.LayoutUpdated+=LayoutUpdated;
            window.Deactivated+=Deactivated;
        }
        if(Program.Arguments.Contains("--trace"))
            _log.EntryAdded+=entry=>Console.WriteLine(entry);
    }
    public void Register(TextBox field, string id, InputRequest request)
    {
        Avalonia.Automation.AutomationProperties.SetName(field,id switch {
            "currency-rate"=>Localization.Locale.Current.Extra("ManualRate"),
            "years"=>Localization.Locale.Current.Text("YearsLabel.Text"),
            "months"=>Localization.Locale.Current.Text("MonthsLabel.Text"),
            "days"=>Localization.Locale.Current.Text("DaysLabel.Text"),_=>id });
        _registrations.Add(new TextInputAdapter(field,id,request,_log));
        if(_bridge is null) return;
        _fields.Add(field);
        // Observe before the bridge: detaching a popup can remove its Window before
        // the bridge can report an empty area. End its context while it still exists.
        field.GotFocus+=GotFocus;
        field.LostFocus+=LostFocus;
        field.DetachedFromVisualTree+=Detached;
        _registrations.Add(_bridge.Register(field,request));
        if(_clipboard is not null) _registrations.Add(new TextClipboardBridge(field,_log,_clipboard));
        _registrations.Add(new PhoneTextMenu(field));
    }
    public async Task<bool> CopyAsync(string text)
    {
        if(_clipboard is not null) return await _clipboard.WriteTextAsync(text);
        return _window.Clipboard is {} clipboard && await ClipboardTextWriter.TryWriteAsync(clipboard,text,_log);
    }
    public Task HideAsync()
    {
        if(_disposed || _bridge is null) return Task.CompletedTask;
        var field=_focused;
        _focused=null;
        _area=default;
        ApplyInset();
        // Deactivate commits composition, invalidates late callbacks, and serializes
        // native hide before a subsequent field activation. A hide request alone
        // neither releases the context nor guarantees an InputAreaChanged callback.
        var dismissal=_bridge.DeactivateAsync();
        if(field?.IsFocused==true) _window.FocusManager?.Focus(null);
        return dismissal;
    }
    public async Task<string?> ReadAsync()
    {
        if(_clipboard is not null) return await _clipboard.ReadTextAsync();
        return _window.Clipboard is {} clipboard ? await clipboard.TryGetTextAsync() : null;
    }
    private void SizeChanged(object? sender, SizeChangedEventArgs e)=>ApplyInset();
    private void GotFocus(object? sender, FocusChangedEventArgs e) { _focused=(TextBox)sender!; }
    private void LostFocus(object? sender, FocusChangedEventArgs e)
    {
        // Allow a transfer between registered editors or a transient clipboard menu.
        var field=(TextBox)sender!;
        Dispatcher.UIThread.Post(()=>
        {
            if(!_disposed && _focused==field && !field.IsFocused) _=HideAsync();
        },DispatcherPriority.Input);
    }
    private void Detached(object? sender, VisualTreeAttachmentEventArgs e)
    { if(_focused==sender) _=HideAsync(); }
    private bool CanEdit(TextBox field) => field.IsEffectivelyVisible && field.IsEffectivelyEnabled &&
        !field.IsReadOnly && TopLevel.GetTopLevel(field)==_window;
    private void LayoutUpdated(object? sender, EventArgs e)
    { if(_focused is {} field && !CanEdit(field)) _=HideAsync(); }
    private void Deactivated(object? sender, EventArgs e) { _=HideAsync(); }
    private void AreaChanged(object? sender, InputArea area)
    {
        if(_disposed || sender!=_window) return;
        _area=_focused is {} field && CanEdit(field)?area:default;
        ApplyInset();
    }
    private void ApplyInset()
    {
        if(_disposed) return;
        var inset=_focused is {} editor && CanEdit(editor)?MaliitLayout.BottomInset(_area,new PixelRect(_window.PointToScreen(default),PixelSize.FromSize(_window.ClientSize,_window.RenderScaling)),_window.RenderScaling):0;
        _root.Padding=new Thickness(_originalPadding.Left,_originalPadding.Top,_originalPadding.Right,_originalPadding.Bottom+inset);
        if(_focused is {} field) Dispatcher.UIThread.Post(()=> { if(!_disposed && _focused==field && field.IsFocused && CanEdit(field)) field.BringIntoView(); },DispatcherPriority.Loaded);
    }
    public void Dispose()
    {
        if(_disposed) return;
        _=HideAsync();
        _disposed=true;
        _window.SizeChanged-=SizeChanged;
        _window.LayoutUpdated-=LayoutUpdated;
        _window.Deactivated-=Deactivated;
        if(_bridge is not null) _bridge.InputAreaChanged-=AreaChanged;
        foreach(var field in _fields)
        {
            field.GotFocus-=GotFocus;
            field.LostFocus-=LostFocus;
            field.DetachedFromVisualTree-=Detached;
        }
        foreach(var item in _registrations) item.Dispose();
        _bridge?.Dispose();
    }
}
