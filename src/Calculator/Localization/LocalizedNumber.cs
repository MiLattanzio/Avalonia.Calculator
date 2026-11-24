using System.Globalization;
using System.Text;

namespace AvaloniaCalculator.Localization;

/// <summary>Validates localized input without converting calculator values to floating point.</summary>
public static class LocalizedNumber
{
    public static string Digits(string text)
    {
        var result=new StringBuilder();
        foreach(var c in text)
        {
            if(c is '\u061c' or '\u200e' or '\u200f' or >= '\u202a' and <= '\u202e' or >= '\u2066' and <= '\u2069') continue;
            int digit=CharUnicodeInfo.GetDecimalDigitValue(c);
            result.Append(digit>=0?(char)('0'+digit):c=='−'?'-':c);
        }
        return result.ToString();
    }
    public static bool TryNormalize(string? input, Locale locale, out string value, int radix=10, bool integer=false)
    {
        value="";
        if(string.IsNullOrWhiteSpace(input) || input.Length>256) return false;
        string text=Digits(input).Trim();
        string sign="";
        if(text.StartsWith('-') || text.StartsWith('+')) { sign=text[..1]; text=text[1..]; }
        if(integer && radix!=10)
        {
            text=text.Replace(" ","").ToUpperInvariant();
            if(text.Length==0 || text.Any(c=>"0123456789ABCDEF".IndexOf(c) is var d && (d<0 || d>=radix))) return false;
            value=sign+text; return true;
        }
        var parts=text.Split(['e','E']);
        if(parts.Length>2 || (integer && parts.Length>1)) return false;
        string exponent="";
        if(parts.Length==2)
        {
            var exp=parts[1]; var digits=exp.StartsWith('+')||exp.StartsWith('-')?exp[1..]:exp;
            if(digits.Length is 0 or >5 || digits.Any(c=>!char.IsAsciiDigit(c))) return false;
            exponent="e"+exp;
        }
        var number=parts[0].Split(locale.DecimalSeparator,StringSplitOptions.None);
        if(number.Length>2 || (integer && number.Length>1)) return false;
        var whole=number[0]; var group=locale.Numbers.NumberGroupSeparator;
        if(string.IsNullOrWhiteSpace(group)) { whole=whole.Replace('\u00a0',' ').Replace('\u202f',' '); group=" "; }
        if(group.Length>0 && whole.Contains(group,StringComparison.Ordinal))
        {
            var blocks=whole.Split(group,StringSplitOptions.None); var sizes=locale.Numbers.NumberGroupSizes;
            for(int i=blocks.Length-1,position=0;i>=0;i--,position++)
            {
                int size=sizes[Math.Min(position,sizes.Length-1)];
                if(blocks[i].Length==0 || (i>0 && (size==0 || blocks[i].Length!=size)) || (i==0 && size>0 && blocks[i].Length>size)) return false;
            }
            whole=string.Concat(blocks);
        }
        var fraction=number.Length==2?number[1]:"";
        if(whole.Length+fraction.Length==0 || whole.Any(c=>!char.IsAsciiDigit(c)) || fraction.Any(c=>!char.IsAsciiDigit(c))) return false;
        value=sign+(whole.Length==0?"0":whole)+(number.Length==2?"."+fraction:"")+exponent;
        return true;
    }
    public static bool TryDouble(string? text,Locale locale,out double value)
    {
        value=0;
        return TryNormalize(text,locale,out var normalized) && double.TryParse(normalized,NumberStyles.Float,CultureInfo.InvariantCulture,out value) && double.IsFinite(value);
    }
    public static bool TryInt(string? text,out int value) => int.TryParse(Digits(text??""),NumberStyles.Integer,CultureInfo.InvariantCulture,out value);
}
