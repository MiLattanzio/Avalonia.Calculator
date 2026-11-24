using System.Runtime.InteropServices;
using System.Text.Json;
using CalculationManager;
using AvaloniaCalculator.Localization;
using Microsoft.Win32.SafeHandles;

namespace AvaloniaCalculator.Engine;

public sealed record HistoryEntry(string Expression, string Result);
public sealed record CalculatorSnapshot(string Display, string Expression, bool Error, string[] Memory, HistoryEntry[] History);
public enum CalculatorMode { Standard, Scientific, Programmer }

/// <summary>Owns the original Uno/Windows Calculator engine. Use only on the UI thread.</summary>
public sealed class CalculatorEngine : IDisposable
{
    private readonly EngineHandle _handle;
    private readonly Locale _locale;
    public int Radix { get; private set; } = 10;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public CalculatorMode Mode { get; private set; }
    public CalculatorSnapshot State { get; private set; }
    public event Action? Changed;
    public CalculatorEngine(Locale? locale=null) { _locale=locale ?? Locale.Current; _handle=new EngineHandle(_locale); if (_handle.IsInvalid) throw new InvalidOperationException("Cannot create calculator engine."); State = Read(); }
    private CalculatorSnapshot Read() => JsonSerializer.Deserialize<CalculatorSnapshot>(ReadString(Native.calculator_snapshot(_handle)), JsonOptions)!;
    private static string ReadString(IntPtr value) => value == IntPtr.Zero ? throw new InvalidOperationException("Native engine failed.") : Marshal.PtrToStringUTF8(value)!;
    private void Update(int status) { if (status != 0) throw new InvalidOperationException(_locale.EngineText("100")); State = Read(); Changed?.Invoke(); }
    public void Send(Command command) { if((int)command is >=313 and <=316) Radix=new[]{16,10,8,2}[(int)command-313]; Update(Native.calculator_command(_handle, (int)command)); }
    public void SetMode(CalculatorMode mode) { Mode=mode; Radix=10; Update(Native.calculator_mode(_handle, (int)mode)); }
    public void Memory(int action, int index = 0) => Update(Native.calculator_memory(_handle, action, index));
    public void ClearHistory() => Update(Native.calculator_clear_history(_handle));
    public string InBase(int radix) => ReadString(Native.calculator_radix(_handle, radix));
    public string CopyText => State.Error ? State.Display : Mode==CalculatorMode.Programmer && Radix!=10 ?
        State.Display.Replace(" ","") : State.Display.Replace(_locale.Numbers.NumberGroupSeparator, "");

    // Validate the whole localized input before touching the current calculation.
    public bool EnterNumber(string text)
    {
        if(!LocalizedNumber.TryNormalize(text,_locale,out var normalized,Radix,Mode==CalculatorMode.Programmer)) return false;
        var parts=Mode==CalculatorMode.Programmer?new[]{normalized}:normalized.Split('e');
        var mantissa=parts[0]; bool negative=mantissa.StartsWith('-');
        if(negative || mantissa.StartsWith('+')) mantissa=mantissa[1..];
        Send(Command.CommandCENTR);
        foreach(var c in mantissa) Send(c=='.'?Command.CommandPNT:(Command)(130+"0123456789ABCDEF".IndexOf(c)));
        if(negative) Send(Command.CommandSIGN);
        if(parts.Length==2)
        {
            Send(Command.CommandEXP);
            foreach(var c in parts[1].TrimStart('+','-')) Send((Command)(130+c-'0'));
            if(parts[1].StartsWith('-')) Send(Command.CommandSIGN);
        }
        return true;
    }
    public void Dispose() => _handle.Dispose();
    private sealed class EngineHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public EngineHandle(Locale locale) : base(true)
        {
            var resources=locale.EngineStrings(); var keys=new List<IntPtr>(); var values=new List<IntPtr>();
            try
            {
                foreach(var pair in resources) { keys.Add(Marshal.StringToCoTaskMemUTF8(pair.Key)); values.Add(Marshal.StringToCoTaskMemUTF8(pair.Value)); }
                SetHandle(Native.calculator_create_localized(keys.ToArray(),values.ToArray(),keys.Count));
            }
            finally { foreach(var pointer in keys.Concat(values)) Marshal.FreeCoTaskMem(pointer); }
        }
        protected override bool ReleaseHandle() { Native.calculator_destroy(handle); return true; }
    }
    private static class Native
    {
        private const string Library="CalculatorEngine";
        [DllImport(Library)] internal static extern IntPtr calculator_create_localized([In] IntPtr[] keys,[In] IntPtr[] values,int count);
        [DllImport(Library)] internal static extern void calculator_destroy(IntPtr handle);
        [DllImport(Library)] internal static extern IntPtr calculator_snapshot(EngineHandle handle);
        [DllImport(Library)] internal static extern int calculator_command(EngineHandle handle,int command);
        [DllImport(Library)] internal static extern int calculator_mode(EngineHandle handle,int mode);
        [DllImport(Library)] internal static extern int calculator_memory(EngineHandle handle,int action,int index);
        [DllImport(Library)] internal static extern IntPtr calculator_radix(EngineHandle handle,int radix);
        [DllImport(Library)] internal static extern int calculator_clear_history(EngineHandle handle);
    }
}
