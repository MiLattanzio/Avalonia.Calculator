using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AvaloniaCalculator.Localization;

/// <summary>Resources and POSIX locale categories captured once, before creating controls or engines.</summary>
public sealed class Locale
{
    private sealed record Bundle(Dictionary<string,string> Strings, Dictionary<string,string> Engine);
    private static readonly Assembly Assembly = typeof(Locale).Assembly;
    private const string Prefix = "AvaloniaCalculator.Localization.Resources.";
    public static readonly string[] SupportedLanguages = Assembly.GetManifestResourceNames()
        .Where(n=>n.StartsWith(Prefix,StringComparison.Ordinal)).Select(n=>n[Prefix.Length..^5]).Order().ToArray();
    private static readonly Bundle English = Load("en-US");
    public static Locale Current { get; private set; } = new("en-US");
    private readonly Bundle _resources;
    public string Language { get; }
    public CultureInfo UICulture { get; }
    public CultureInfo Culture { get; }
    public NumberFormatInfo Numbers => Culture.NumberFormat;
    public string DecimalSeparator => Numbers.NumberDecimalSeparator;
    public bool IsRightToLeft => UICulture.TextInfo.IsRightToLeft;
    public Locale(string language, string? numeric = null, string? dates = null)
    {
        Language = Match(language) ?? "en-US";
        UICulture = CultureInfo.GetCultureInfo(Language);
        _resources = Language=="en-US" ? English : Load(Language);
        Culture = (CultureInfo)GetCulture(dates ?? language).Clone();
        // DateMath, Uno, and Avalonia DatePicker all operate on Gregorian year/month/day.
        if(Culture.OptionalCalendars.Any(c=>c is GregorianCalendar)) Culture.DateTimeFormat.Calendar=new GregorianCalendar();
        Culture.NumberFormat = (NumberFormatInfo)GetCulture(numeric ?? language).NumberFormat.Clone();
    }
    private static Bundle Load(string name)
    {
        using var stream=Assembly.GetManifestResourceStream(Prefix+name+".json") ?? throw new InvalidOperationException("Missing locale: "+name);
        return JsonSerializer.Deserialize<Bundle>(stream)!;
    }
    public static string Normalize(string value)
    {
        var parts=value.Trim().Split('@',2);
        var tag=parts[0].Split('.')[0].Replace('_','-');
        if(tag is "C" or "POSIX" or "") return "en-US";
        if(parts.Length==2 && parts[1]=="latin" && !tag.Contains("Latn",StringComparison.OrdinalIgnoreCase))
        { var index=tag.IndexOf('-'); tag=index<0?tag+"-Latn":tag.Insert(index,"-Latn"); }
        return tag;
    }
    private static CultureInfo GetCulture(string name)
    {
        try { var culture=CultureInfo.GetCultureInfo(Normalize(name)); return culture.IsNeutralCulture?CultureInfo.CreateSpecificCulture(culture.Name):culture; }
        catch(CultureNotFoundException) { return CultureInfo.GetCultureInfo("en-US"); }
    }
    public static string? Match(string name)
    {
        name=Normalize(name);
        var exact=SupportedLanguages.FirstOrDefault(s=>s.Equals(name,StringComparison.OrdinalIgnoreCase));
        if(exact is not null) return exact;
        var language=name.Split('-')[0].ToLowerInvariant();
        if(language=="zh") return name.Contains("Hant",StringComparison.OrdinalIgnoreCase) || name.EndsWith("-HK",StringComparison.OrdinalIgnoreCase) || name.EndsWith("-MO",StringComparison.OrdinalIgnoreCase)?"zh-TW":"zh-CN";
        var preferred=language switch { "en"=>"en-US","es"=>"es-ES","fr"=>"fr-FR","pt"=>"pt-PT","no"=>"nb-NO",_=>null };
        return preferred ?? SupportedLanguages.FirstOrDefault(s=>s.StartsWith(language+"-",StringComparison.OrdinalIgnoreCase));
    }
    public static Locale FromEnvironment(Func<string,string?> env, string? overrideLocale=null)
    {
        if(overrideLocale is not null) return new Locale(overrideLocale);
        string? Value(string key) => string.IsNullOrWhiteSpace(env(key))?null:env(key);
        var all=Value("LC_ALL"); var lang=Value("LANG") ?? CultureInfo.CurrentCulture.Name;
        var messages=all ?? Value("LC_MESSAGES") ?? lang;
        var candidates=messages.Split('.')[0] is "C" or "POSIX" ? new[]{"en-US"} :
            (Value("LANGUAGE")?.Split(':') ?? []).Concat(new[]{messages}).ToArray();
        var ui=candidates.Select(Match).FirstOrDefault(s=>s is not null) ?? "en-US";
        return new Locale(ui,all ?? Value("LC_NUMERIC") ?? lang,all ?? Value("LC_TIME") ?? lang);
    }
    public static void Use(Locale locale)
    {
        Current=locale;
        CultureInfo.CurrentCulture=CultureInfo.DefaultThreadCurrentCulture=locale.Culture;
        CultureInfo.CurrentUICulture=CultureInfo.DefaultThreadCurrentUICulture=locale.UICulture;
    }
    public string Text(string key) => _resources.Strings.GetValueOrDefault(key) ?? English.Strings.GetValueOrDefault(key) ?? throw new KeyNotFoundException(key);
    public string Accessible(string element) => Text(element+".[using:Windows.UI.Xaml.Automation]AutomationProperties.Name");
    public string EngineText(string key) => _resources.Engine.GetValueOrDefault(key) ?? English.Engine[key];
    public string Format(string key, params object[] arguments) => Regex.Replace(Text(key), @"%([1-9][0-9]*)", m=>
        int.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture) is var i && i<=arguments.Length?Convert.ToString(arguments[i-1],Culture)??"":m.Value);
    public Dictionary<string,string> EngineStrings()
    {
        var strings=new Dictionary<string,string>(English.Engine);
        foreach(var pair in _resources.Engine) strings[pair.Key]=pair.Value;
        strings["4"]=DecimalSeparator; strings["sDecimal"]=DecimalSeparator; strings["sThousand"]=Numbers.NumberGroupSeparator;
        var groups=Numbers.NumberGroupSizes;
        // Win32 grouping repeats its last size when terminated by zero; .NET repeats it without zero.
        strings["sGrouping"]=string.Join(';',groups.Length==1 && groups[0]==0?new[]{0,0}:groups[^1]==0?groups[..^1]:groups.Concat(new[]{0}));
        return strings;
    }
    public string DateDifference(int years,int months,int days,int totalDays)
    {
        string Part(int value,string key)=>value.ToString(Culture)+" "+Text("Date_"+key+(value==1?"":"s"));
        return totalDays==0?Text("Date_SameDates"):string.Join(Culture.TextInfo.ListSeparator+" ",new[]{Part(years,"Year"),Part(months,"Month"),Part(days,"Day")})+"\n"+Part(totalDays,"Day");
    }
    public string Extra(string key) => ExtraStrings.Get(Language,key);
}
