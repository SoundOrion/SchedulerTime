using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassLibrary;

public interface IHolidaysProvider
{
    SortedSet<DateTime> GetHolidays();
}

public class HolidaysService : BackgroundService, IHolidaysProvider
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<HolidaysService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(24); // 1日ごとに更新
    private SortedSet<DateTime> _holidays = new();

    public HolidaysService(IDbConnection dbConnection, ILogger<HolidaysService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public SortedSet<DateTime> GetHolidays()
    {
        return _holidays;
    }

    private void LoadHolidaysFromDatabase()
    {
        try
        {
            var holidaySet = new SortedSet<DateTime>();

            string query = "SELECT HolidayDate FROM Holidays";
            var command = _dbConnection.CreateCommand();
            command.CommandText = query;

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (DateTime.TryParse(reader["HolidayDate"].ToString(), out DateTime holiday))
                    {
                        holidaySet.Add(holiday);
                    }
                }
            }

            _holidays = holidaySet;
            _logger.LogInformation("祝日データを更新しました: {Count}件", _holidays.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "祝日データの取得に失敗しました");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            LoadHolidaysFromDatabase();
            await Task.Delay(_updateInterval, stoppingToken);
        }
    }
}

public class Scheduler
{
    private readonly Lazy<SortedSet<DateTime>> holidays;
    private readonly DateTime _baseDate;

    public Scheduler(DateTime baseDate, IHolidaysProvider holidaysProvider)
    {
        _baseDate = baseDate;
        holidays = new Lazy<SortedSet<DateTime>>(holidaysProvider.GetHolidays);
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

    private DateTime GetNextWeeklyExecution(DateTime now, TimeSpan executionTime, bool isBusinessDay, HashSet<int> daysOfWeek, TimeSpan offset)
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
