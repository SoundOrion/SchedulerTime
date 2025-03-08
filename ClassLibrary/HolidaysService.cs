using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ClassLibrary;

public class HolidaysService : BackgroundService, IHolidaysProvider
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<HolidaysService> _logger;
    private readonly TimeSpan _updateInterval = Timeout.InfiniteTimeSpan;   // TimeSpan.FromHours(24); // 1日ごとに更新
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
