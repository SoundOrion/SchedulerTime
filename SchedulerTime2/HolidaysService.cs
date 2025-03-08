using ClassLibrary;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class HolidaysService : IHostedService, IHolidaysProvider
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<HolidaysService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(24); // 24時間ごとに更新
    private SortedSet<DateTime> _holidays = new();
    private Timer? _timer;

    public HolidaysService(IDbConnection dbConnection, ILogger<HolidaysService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public SortedSet<DateTime> GetHolidays()
    {
        return _holidays;
    }

    private void LoadHolidaysFromDatabase(object? state)
    {
        try
        {
            var holidaySet = new SortedSet<DateTime>();

            string query = "SELECT HolidayDate FROM Holidays";
            using var command = _dbConnection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (DateTime.TryParse(reader["HolidayDate"].ToString(), out DateTime holiday))
                {
                    holidaySet.Add(holiday);
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HolidaysService を開始します。");
        LoadHolidaysFromDatabase(null); // 起動時に即時更新
        _timer = new Timer(LoadHolidaysFromDatabase, null, _updateInterval, _updateInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HolidaysService を停止します。");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }



    private async Task LoadHolidaysFromDatabaseAsync(object? state)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var holidaySet = new SortedSet<DateTime>();

            const string query = "SELECT HolidayDate FROM Holidays";

            // DbCommand を使用する
            using var command = _dbConnection.CreateCommand() as DbCommand;
            if (command is null)
            {
                _logger.LogError("データベースコマンドの作成に失敗しました");
                return;
            }

            command.CommandText = query;

            // 非同期にデータ取得
            // DbConnection の場合は OpenAsync を使う
            if (_dbConnection is DbConnection dbConn && dbConn.State != ConnectionState.Open)
            {
                await dbConn.OpenAsync().ConfigureAwait(false);
            }
            else if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open(); // 非同期が使えない場合は同期メソッドを使用
            }

            //using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            // `SequentialAccess` を使ってデータ取得を最適化
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess).ConfigureAwait(false);

            await foreach (var holiday in ReadHolidaysAsync(reader))
            {
                holidaySet.Add(holiday);
            }

            _holidays = holidaySet;
            stopwatch.Stop();

            _logger.LogInformation("祝日データを更新しました: {Count}件 (処理時間: {Elapsed}ms)", _holidays.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "祝日データの取得に失敗しました");
        }
    }

    private async IAsyncEnumerable<DateTime> ReadHolidaysAsync(DbDataReader reader)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader["HolidayDate"] is DateTime holiday)
            {
                yield return holiday;
            }
            else if (DateTime.TryParse(reader["HolidayDate"]?.ToString(), out var parsedHoliday))
            {
                yield return parsedHoliday;
            }
        }
    }

    //private async IAsyncEnumerable<DateTime> ReadHolidaysAsync(DbDataReader reader)
    //{
    //    while (await reader.ReadAsync().ConfigureAwait(false))
    //    {
    //        // `GetDateTime(0)` を使い型変換を最小限に
    //        yield return reader.GetDateTime(0);
    //    }
    //}
}
