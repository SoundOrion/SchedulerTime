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

    public static DateTime GetNextExecutionTime(string type, string property, string rolldate = "0:00")
    {
        TimeSpan offset = TimeSpan.Parse(rolldate);

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
            "MONTHLY" => GetNextMonthlyExecution(now, int.Parse(parts[2]), executionTime, isBusinessDay, offset),
            "MONTHLY_LAST" => GetNextMonthlyLastExecution(now, executionTime, isBusinessDay, offset),
            "DAILY" => GetNextDailyExecution(now, executionTime, isBusinessDay, offset),
            "WEEKLY" => GetNextWeeklyExecution(now, executionTime, isBusinessDay, ParseWeeklyDays(parts[2]), offset),
            _ => throw new ArgumentException($"無効なスケジュールタイプ: {type}")
        };
    }

    private static DateTime GetNextTimelyExecution(DateTime now, TimeSpan interval)
    {
        return now.Add(interval);
    }

    private static DateTime GetNextDailyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, TimeSpan offset)
    {
        DateTime nextExecution = now.Date.Add(executionTime);

        if (now > nextExecution)
            nextExecution = nextExecution.AddDays(1);

        return isBusinessDay ? GetNextBusinessDay(nextExecution, offset) : nextExecution;
    }

    private static DateTime GetNextWeeklyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, HashSet<int> daysOfWeek, TimeSpan offset)
    {
        DateTime nextExecution = now.Date.Add(executionTime);

        while (!daysOfWeek.Contains((int)nextExecution.DayOfWeek) || (isBusinessDay && !IsBusinessDay(nextExecution, offset)))
        {
            nextExecution = nextExecution.AddDays(1);
        };   

        return nextExecution;
    }

    private static DateTime GetNextMonthlyExecution(DateTime now, int day, TimeSpan executionTime, bool isBusinessDay, TimeSpan offset)
    {
        DateTime nextExecution = new DateTime(now.Year, now.Month, Math.Min(day, DateTime.DaysInMonth(now.Year, now.Month))).Add(executionTime);

        if (now > nextExecution)
            nextExecution = nextExecution.AddMonths(1);

        return isBusinessDay ? GetNextBusinessDay(nextExecution, offset) : nextExecution;
    }

    private static DateTime GetNextMonthlyLastExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, TimeSpan offset)
    {
        DateTime nextExecution = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).Add(executionTime);

        if (now > nextExecution)
            nextExecution = new DateTime(now.AddMonths(1).Year, now.AddMonths(1).Month, DateTime.DaysInMonth(now.AddMonths(1).Year, now.AddMonths(1).Month)).Add(executionTime);

        return isBusinessDay ? GetLastBusinessDay(nextExecution, offset) : nextExecution;
    }

    private static DateTime GetNextBusinessDay(DateTime date, TimeSpan offset)
    {
        while (!IsBusinessDay(date, offset))
        {
            date = date.AddDays(1);
        };

        return date;
    }

    private static DateTime GetLastBusinessDay(DateTime date, TimeSpan offset)
    {
        while (!IsBusinessDay(date, offset))
        {
            date = date.AddDays(-1);
        };
        
        return date;
    }

    private static bool IsBusinessDay(DateTime date, TimeSpan offset)
    {
        DateTime adjustedDate = date - offset;
        return !IsWeekend(adjustedDate) && !holidays.Value.Contains(adjustedDate.Date);
    }

    private static bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static HashSet<int> ParseWeeklyDays(string days)
    {
        if (!Regex.IsMatch(days, "^[0-6]+$"))
            throw new ArgumentException($"不正な曜日情報: {days}");

        return days.Select(c => c - '0').ToHashSet();
    }

    public static string GenerateDescription(string type, string property)
    {
        string[] parts = property.Split('|');

        if (type == "ONDEMAND")
            return "即時実行";

        if (type == "TIMELY")
        {
            if (!TimeSpan.TryParse(parts[0], out TimeSpan interval))
                throw new ArgumentException($"[エラー] 不正な間隔フォーマット: {property}");
            return $"指定間隔ごとに実行 ({interval.Hours}時間{interval.Minutes}分)";
        }

        if (parts.Length < 2)
            throw new ArgumentException($"[エラー] 不正なフォーマット: {property}");

        bool isBusinessDay = parts[1] == "1";
        string businessDayText = isBusinessDay ? " (営業日)" : " (カレンダー日)";

        if (!TimeSpan.TryParse(parts[0], out TimeSpan executionTime))
            throw new ArgumentException($"[エラー] 不正な時間フォーマット: {parts[0]}");

        string timeString = $"{executionTime.Hours:D2}:{executionTime.Minutes:D2}";

        return type switch
        {
            "DAILY" => $"毎日 {timeString} に実行{businessDayText}",
            "WEEKLY" => GenerateWeeklyDescription(timeString, parts, isBusinessDay),
            "MONTHLY" => GenerateMonthlyDescription(timeString, parts, isBusinessDay),
            "MONTHLY_LAST" => $"毎月最終日 {timeString} に実行{businessDayText}",
            _ => throw new ArgumentException($"[エラー] 無効なスケジュールタイプ: {type}")
        };
    }

    // 週次スケジュールの説明を生成
    private static string GenerateWeeklyDescription(string timeString, string[] parts, bool isBusinessDay)
    {
        HashSet<int> daysOfWeek = ParseWeeklyDays(parts[2]);
        string businessDayText = isBusinessDay ? " (営業日)" : " (カレンダー日)";
        string[] weekDays = { "日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日" };
        string dayList = string.Join(", ", daysOfWeek.Select(d => weekDays[d]));
        return $"毎週 {dayList} の {timeString} に実行{businessDayText}";
    }

    // 月次スケジュールの説明を生成
    private static string GenerateMonthlyDescription(string timeString, string[] parts, bool isBusinessDay)
    {
        if (!int.TryParse(parts[2], out int day) || day < 1 || day > 31)
            throw new ArgumentException($"[エラー] 不正な日付: {parts[2]}");

        string businessDayText = isBusinessDay ? " (営業日)" : " (カレンダー日)";
        return $"毎月 {day}日 {timeString} に実行{businessDayText}";
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
        Console.WriteLine("DAILY 営業日: " + Scheduler.GetNextExecutionTime("DAILY", "02:29|1"));
        Console.WriteLine("DAILY 営業日: " + Scheduler.GetNextExecutionTime("DAILY", "19:30|1"));
        Console.WriteLine("DAILY カレンダー: " + Scheduler.GetNextExecutionTime("DAILY", "02:29|0"));
        Console.WriteLine("WEEKLY 営業日: " + Scheduler.GetNextExecutionTime("WEEKLY", "03:00|1|156"));
        Console.WriteLine("WEEKLY カレンダー: " + Scheduler.GetNextExecutionTime("WEEKLY", "18:00|0|034"));
        Console.WriteLine("TIMELY（1時間ごと）: " + Scheduler.GetNextExecutionTime("TIMELY", "01:00"));
        Console.WriteLine("TIMELY（30分ごと）: " + Scheduler.GetNextExecutionTime("TIMELY", "00:30"));
        Console.WriteLine("MONTHLY（毎月5日 9:00）: " + Scheduler.GetNextExecutionTime("MONTHLY", "09:00|0|5"));
        Console.WriteLine("MONTHLY（毎月20日 9:00）: " + Scheduler.GetNextExecutionTime("MONTHLY", "9:00|0|20"));
        Console.WriteLine("MONTHLY（毎月20日 18:00、営業日）: " + Scheduler.GetNextExecutionTime("MONTHLY", "18:00|1|20"));
        Console.WriteLine("MONTHLY_LAST（毎月末日 23:59）: " + Scheduler.GetNextExecutionTime("MONTHLY_LAST", "23:59|0"));
        Console.WriteLine("MONTHLY_LAST_BIZ（毎月最終営業日 18:00）: " + Scheduler.GetNextExecutionTime("MONTHLY_LAST", "18:00|1"));

        string[] testCases = {
            "ONDEMAND|",
            "TIMELY|01:00",
            "DAILY|19:30|1",
            "DAILY|17:00|0",
            "WEEKLY|17:00|1|15",
            "WEEKLY|17:00|0|0123456",
            "WEEKLY|18:00|0|034",
            "MONTHLY|09:00|0|5",
            "MONTHLY|09:00|0|20",
            "MONTHLY|18:00|1|20",
            "MONTHLY_LAST|23:59|0",
            "MONTHLY_LAST|18:00|1"
        };

        foreach (string testCase in testCases)
        {
            string[] parts = testCase.Split('|', 2);
            string type = parts[0];
            string property = parts.Length > 1 ? parts[1] : "";

            Console.WriteLine($"スケジュール: {testCase}");
            Console.WriteLine($"説明: {Scheduler.GenerateDescription(type, property)}");
            Console.WriteLine();
        }
    }
}
