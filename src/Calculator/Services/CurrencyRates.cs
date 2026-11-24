using System.Globalization;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace AvaloniaCalculator.Services;
public sealed record CurrencyTable(DateOnly Date, IReadOnlyDictionary<string,double> Rates)
{
    public double Rate(string from,string to) => Rates[to]/Rates[from];
}
public static class CurrencyRates
{
    public const string Source="https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    public static async Task<CurrencyTable> DownloadAsync(CancellationToken cancellationToken=default)
    {
        using var client=new HttpClient { Timeout=TimeSpan.FromSeconds(15), MaxResponseContentBufferSize=1024*1024 };
        var xml=await client.GetStringAsync(Source,cancellationToken);
        return Parse(xml);
    }
    public static CurrencyTable Parse(string xml)
    {
        using var reader=XmlReader.Create(new StringReader(xml),new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null, MaxCharactersInDocument=1024*1024 });
        var doc=XDocument.Load(reader);
        var day=doc.Descendants().Single(e=>e.Name.LocalName=="Cube"&&e.Attribute("time") is not null);
        var date=DateOnly.ParseExact((string)day.Attribute("time")!,"yyyy-MM-dd",CultureInfo.InvariantCulture);
        var rates=new Dictionary<string,double>(StringComparer.Ordinal) { ["EUR"]=1 };
        foreach(var entry in day.Elements())
        {
            var code=(string?)entry.Attribute("currency");
            if(code is null || code.Length!=3 || code.Any(c=>c<'A'||c>'Z') ||
                !double.TryParse((string?)entry.Attribute("rate"),NumberStyles.Float,CultureInfo.InvariantCulture,out var rate)||!double.IsFinite(rate)||rate<=0)
                throw new FormatException("Invalid exchange rate data.");
            rates.Add(code,rate);
        }
        if(rates.Count<2) throw new FormatException("No exchange rates.");
        return new CurrencyTable(date,rates);
    }
}
