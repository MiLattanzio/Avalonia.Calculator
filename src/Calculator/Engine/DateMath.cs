namespace AvaloniaCalculator.Engine;
public static class DateMath
{
    public static DateOnly Add(DateOnly date,int years,int months,int days,bool subtract=false) => subtract
        ? date.AddDays(-days).AddMonths(-months).AddYears(-years)
        : date.AddYears(years).AddMonths(months).AddDays(days);
    public static (int Years,int Months,int Days,int TotalDays) Difference(DateOnly first,DateOnly second)
    {
        if(first>second) (first,second)=(second,first);
        int years=second.Year-first.Year;
        if(first.AddYears(years)>second) years--;
        var cursor=first.AddYears(years);
        int months=(second.Year-cursor.Year)*12+second.Month-cursor.Month;
        if(cursor.AddMonths(months)>second) months--;
        return (years,months,second.DayNumber-cursor.AddMonths(months).DayNumber,second.DayNumber-first.DayNumber);
    }
}
