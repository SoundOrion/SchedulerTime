using System.Text.RegularExpressions;

namespace ClassLibrary;

public class Scheduler
{
    private readonly Lazy<SortedSet<DateTime>> holidays = new Lazy<SortedSet<DateTime>>(() =>
    {
        var holidaySet = new SortedSet<DateTime>();
        for (int year = DateTime.Now.Year; year <= DateTime.Now.Year + 5; year++)
        {
            holidaySet.Add(new DateTime(year, 1, 1));
            holidaySet.Add(new DateTime(year, 3, 20));
            holidaySet.Add(new DateTime(year, 12, 25));
        }
        return holidaySet;
    });

    private readonly DateTime _baseDate;

    public Scheduler(DateTime baseDate)
    {
        _baseDate = baseDate;
    }

    public DateTime GetNextExecutionTime(string type, string property, string rolldate)
    {
        DateTime now = _baseDate;

        if (type == "ONDEMAND")
            return now; // 即時実行

        TimeSpan offset = TimeSpan.Parse(rolldate);

        string[] parts = property.Split('|');

        if (parts.Length < 2)
            throw new ArgumentException($"不正なフォーマットです: {property}");

        bool isBusinessDay = parts[1] == "1";

        if (!TimeSpan.TryParse(parts[0], out TimeSpan executionTime))
            throw new ArgumentException($"不正な時間フォーマット: {parts[0]}");

        return type switch
        {
            "DAILY" => GetNextDailyExecution(now, executionTime, isBusinessDay, offset),
            "WEEKLY" => GetNextWeeklyExecution(now, executionTime, isBusinessDay, ParseWeeklyDays(parts[2]), offset),
            _ => throw new ArgumentException($"無効なスケジュールタイプ: {type}")
        };
    }


    private DateTime GetNextDailyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, TimeSpan offset)
    {
        DateTime nextExecution = now.Date.Add(executionTime);

        if (now > nextExecution)
            nextExecution = nextExecution.AddDays(1);

        return isBusinessDay ? GetNextBusinessDay(nextExecution, offset) : nextExecution;
    }

    private  DateTime GetNextWeeklyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, HashSet<int> daysOfWeek, TimeSpan offset)
    {
        DateTime nextExecution = now.Date.Add(executionTime);

        while (!daysOfWeek.Contains((int)nextExecution.DayOfWeek) || (isBusinessDay && !IsBusinessDay(nextExecution, offset)))
        {
            nextExecution = nextExecution.AddDays(1);
        };

        return nextExecution;
    }

    private DateTime GetNextBusinessDay(DateTime date, TimeSpan offset)
    {
        while (!IsBusinessDay(date, offset))
        {
            date = date.AddDays(1);
        };

        return date;
    }

    private bool IsBusinessDay(DateTime date, TimeSpan offset)
    {
        DateTime adjustedDate = date - offset;
        return !IsWeekend(adjustedDate) && !holidays.Value.Contains(adjustedDate.Date);
    }

    private bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private HashSet<int> ParseWeeklyDays(string days)
    {
        if (!Regex.IsMatch(days, "^[0-6]+$"))
            throw new ArgumentException($"不正な曜日情報: {days}");

        return days.Select(c => c - '0').ToHashSet();
    }

}
