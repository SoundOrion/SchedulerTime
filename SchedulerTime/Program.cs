using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class Scheduler
{
    private static readonly Lazy<SortedSet<DateTime>> holidays = new Lazy<SortedSet<DateTime>>(() =>
    {
        var holidaySet = new SortedSet<DateTime>();
        for (int year = DateTime.Now.Year; year <= DateTime.Now.Year + 5; year++)
        {
            holidaySet.Add(new DateTime(year, 1, 1));  // 元旦
            holidaySet.Add(new DateTime(year, 12, 25)); // クリスマス
        }
        return holidaySet;
    });

    public static void AddHoliday(DateTime holiday)
    {
        holidays.Value.Add(holiday.Date);
    }

    public static DateTime GetNextExecutionTime(string type, string property)
    {
        DateTime now = DateTime.Now;

        if (type == "ONDEMAND")
            return now; // 即時実行

        string[] parts = property.Split('|');

        if (type == "TIMELY")
        {
            if (!TimeSpan.TryParse(parts[0], out TimeSpan interval))
                throw new ArgumentException($"不正な間隔フォーマット: {property}");

            return GetNextTimelyExecution(now, interval);
        }

        if (parts.Length < 2)
            throw new ArgumentException($"不正なフォーマットです: {property}");

        bool isBusinessDay = parts[1] == "1";

        if (!TimeSpan.TryParse(parts[0], out TimeSpan executionTime))
            throw new ArgumentException($"不正な時間フォーマット: {parts[0]}");

        return type switch
        {
            "MONTHLY" => GetNextMonthlyExecution(now, int.Parse(parts[0]), executionTime, isBusinessDay),
            "MONTHLY_LAST" => GetNextMonthlyLastExecution(now, executionTime, isBusinessDay),
            "DAILY" => GetNextDailyExecution(now, executionTime, isBusinessDay),
            "WEEKLY" => GetNextWeeklyExecution(now, executionTime, isBusinessDay, ParseWeeklyDays(parts[2])),
            _ => throw new ArgumentException($"無効なスケジュールタイプ: {type}")
        };
    }

    private static DateTime GetNextTimelyExecution(DateTime now, TimeSpan interval)
    {
        return now.Add(interval);
    }

    private static DateTime GetNextDailyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay)
    {
        DateTime nextExecution = now.Date.Add(executionTime);
        if (now > nextExecution)
            nextExecution = nextExecution.AddDays(1);

        return isBusinessDay ? GetNextBusinessDay(nextExecution) : nextExecution;
    }

    private static DateTime GetNextWeeklyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, HashSet<int> daysOfWeek)
    {
        DateTime nextExecution = now.Date.Add(executionTime);
        if (daysOfWeek.Contains((int)now.DayOfWeek) && now <= nextExecution)
            return nextExecution;

        do
        {
            nextExecution = nextExecution.AddDays(1);
        }
        while (!daysOfWeek.Contains((int)nextExecution.DayOfWeek) || (isBusinessDay && !IsBusinessDay(nextExecution)));

        return nextExecution;
    }

    private static DateTime GetNextMonthlyExecution(DateTime now, int day, TimeSpan executionTime, bool isBusinessDay)
    {
        DateTime nextExecution = new DateTime(now.Year, now.Month, Math.Min(day, DateTime.DaysInMonth(now.Year, now.Month))).Add(executionTime);

        if (now > nextExecution)
            nextExecution = nextExecution.AddMonths(1);

        return isBusinessDay ? GetNextBusinessDay(nextExecution) : nextExecution;
    }

    private static DateTime GetNextMonthlyLastExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay)
    {
        DateTime nextExecution = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).Add(executionTime);

        if (now > nextExecution)
            nextExecution = new DateTime(now.AddMonths(1).Year, now.AddMonths(1).Month, DateTime.DaysInMonth(now.AddMonths(1).Year, now.AddMonths(1).Month)).Add(executionTime);

        return isBusinessDay ? GetLastBusinessDay(nextExecution) : nextExecution;
    }

    private static DateTime GetNextBusinessDay(DateTime date)
    {
        do
        {
            date = date.AddDays(1);
        }
        while (!IsBusinessDay(date));

        return date;
    }

    private static DateTime GetLastBusinessDay(DateTime date)
    {
        do
        {
            date = date.AddDays(-1);
        }
        while (!IsBusinessDay(date));

        return date;
    }

    private static bool IsBusinessDay(DateTime date) => !IsWeekend(date) && !holidays.Value.Contains(date.Date);

    private static bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;


    private static HashSet<int> ParseWeeklyDays(string days)
    {
        if (!Regex.IsMatch(days, "^[0-6]+$"))
            throw new ArgumentException($"不正な曜日情報: {days}");

        return days.Select(c => c - '0').ToHashSet();
    }
}

class Program
{
    static void Main()
    {
        // 追加の祝日設定
        Scheduler.AddHoliday(new DateTime(DateTime.Now.Year, 5, 3)); // ゴールデンウィーク
        Scheduler.AddHoliday(new DateTime(DateTime.Now.Year, 11, 23)); // 勤労感謝の日

        // テストケース
        Console.WriteLine("ONDEMAND: " + Scheduler.GetNextExecutionTime("ONDEMAND", ""));
        Console.WriteLine("DAILY 営業日: " + Scheduler.GetNextExecutionTime("DAILY", "19:29|1"));
        Console.WriteLine("DAILY 営業日: " + Scheduler.GetNextExecutionTime("DAILY", "19:30|1"));
        Console.WriteLine("DAILY カレンダー: " + Scheduler.GetNextExecutionTime("DAILY", "17:00|0"));
        Console.WriteLine("WEEKLY 営業日: " + Scheduler.GetNextExecutionTime("WEEKLY", "17:00|1|15"));
        Console.WriteLine("WEEKLY カレンダー: " + Scheduler.GetNextExecutionTime("WEEKLY", "18:00|0|034"));
        Console.WriteLine("TIMELY（1時間ごと）: " + Scheduler.GetNextExecutionTime("TIMELY", "01:00"));
        Console.WriteLine("TIMELY（30分ごと）: " + Scheduler.GetNextExecutionTime("TIMELY", "00:30"));
        Console.WriteLine("MONTHLY（毎月5日 9:00）: " + Scheduler.GetNextExecutionTime("MONTHLY", "5|09:00|0"));
        Console.WriteLine("MONTHLY（毎月20日 18:00、営業日）: " + Scheduler.GetNextExecutionTime("MONTHLY", "20|18:00|1"));
        Console.WriteLine("MONTHLY_LAST（毎月末日 23:59）: " + Scheduler.GetNextExecutionTime("MONTHLY_LAST", "23:59|0"));
        Console.WriteLine("MONTHLY_LAST_BIZ（毎月最終営業日 18:00）: " + Scheduler.GetNextExecutionTime("MONTHLY_LAST", "18:00|1"));
        Console.WriteLine("ONDEMAND: " + Scheduler.GetNextExecutionTime("ONDEMAND", ""));
        Console.WriteLine("DAILY（営業日）: " + Scheduler.GetNextExecutionTime("DAILY", "19:29|1"));
        Console.WriteLine("WEEKLY（営業日）: " + Scheduler.GetNextExecutionTime("WEEKLY", "17:00|1|15"));
        Console.WriteLine("TIMELY（1時間ごと）: " + Scheduler.GetNextExecutionTime("TIMELY", "01:00"));
        Console.WriteLine("MONTHLY（毎月5日 9:00）: " + Scheduler.GetNextExecutionTime("MONTHLY", "5|09:00|0"));
        Console.WriteLine("MONTHLY_LAST（毎月末日 23:59）: " + Scheduler.GetNextExecutionTime("MONTHLY_LAST", "23:59|0"));
        Console.WriteLine("MONTHLY_LAST_BIZ（毎月最終営業日 18:00）: " + Scheduler.GetNextExecutionTime("MONTHLY_LAST", "18:00|1"));
    }
}
