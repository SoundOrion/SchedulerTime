﻿using System;
using System.Collections.Generic;
using System.Linq;

class Scheduler
{
    public static DateTime GetNextExecutionTime(string type, string property)
    {
        DateTime now = DateTime.Now;

        if (type == "ONDEMAND")
        {
            return now; // 即時実行
        }

        string[] parts = property.Split('|');

        if (parts.Length < 2)
        {
            throw new ArgumentException($"不正なフォーマットです: {property}");
        }

        // 実行時間
        if (!TimeSpan.TryParse(parts[0], out TimeSpan executionTime))
        {
            throw new ArgumentException($"不正な時間フォーマット: {parts[0]}");
        }

        bool isBusinessDay = parts[1] == "1"; // 営業日かどうか

        if (type == "DAILY")
        {
            return GetNextDailyExecution(now, executionTime, isBusinessDay);
        }

        if (type == "WEEKLY")
        {
            //HashSet<int> daysOfWeek = parts[2].Select(c => int.Parse(c.ToString())).ToHashSet();
            HashSet<int> daysOfWeek = parts[2]
                .Select(c => int.TryParse(c.ToString(), out int day) ? (int?)day : null)
                .Where(day => day.HasValue && day >= 0 && day <= 6)
                .Select(day => day.Value)
                .ToHashSet();

            // 不正な値がある場合に例外をスロー
            if (daysOfWeek.Count != parts[2].Length)
            {
                throw new ArgumentException($"不正な曜日情報: {parts[2]}");
            }

            return GetNextWeeklyExecution(now, executionTime, isBusinessDay, daysOfWeek);
        }

        throw new ArgumentException("Invalid type");
    }

    private static DateTime GetNextDailyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay)
    {
        DateTime nextExecution = now.Date.Add(executionTime);

        if (now > nextExecution)
        {
            nextExecution = nextExecution.AddDays(1);
        }

        if (isBusinessDay)
        {
            while (IsWeekend(nextExecution))
            {
                nextExecution = nextExecution.AddDays(1);
            }
        }

        return nextExecution;
    }


    private static DateTime GetNextWeeklyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, HashSet<int> daysOfWeek)
    {
        DateTime nextExecution = now.Date.Add(executionTime);

        // 今日が実行対象の曜日で、まだ実行時間前ならそのまま使う
        if (daysOfWeek.Contains((int)now.DayOfWeek) && now <= nextExecution)
        {
            return nextExecution;
        }

        // 明日以降の実行日を探す
        do
        {
            nextExecution = nextExecution.AddDays(1);
        }
        while (!daysOfWeek.Contains((int)nextExecution.DayOfWeek) || (isBusinessDay && IsWeekend(nextExecution)));

        return nextExecution;
    }

    private static bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }
}

class Program
{
    static void Main()
    {
        // テストケース
        Console.WriteLine("ONDEMAND: " + Scheduler.GetNextExecutionTime("ONDEMAND", ""));
        Console.WriteLine("DAILY 営業日: " + Scheduler.GetNextExecutionTime("DAILY", "19:29|1"));
        Console.WriteLine("DAILY 営業日: " + Scheduler.GetNextExecutionTime("DAILY", "19:30|1"));
        Console.WriteLine("DAILY カレンダー: " + Scheduler.GetNextExecutionTime("DAILY", "17:00|0"));
        Console.WriteLine("WEEKLY 営業日: " + Scheduler.GetNextExecutionTime("WEEKLY", "17:00|1|15"));
        Console.WriteLine("WEEKLY カレンダー: " + Scheduler.GetNextExecutionTime("WEEKLY", "18:00|0|034"));
    }
}
