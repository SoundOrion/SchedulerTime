`AddHostedService` を使って、定期的にデータベースから祝日データを取得し、それを `Scheduler` に供給する方法を説明します。

---

### **1. `IHolidaysProvider` インターフェース**
まず、祝日データを取得するためのインターフェースを定義します。

```csharp
public interface IHolidaysProvider
{
    SortedSet<DateTime> GetHolidays();
}
```

---

### **2. `HolidaysService`（Hosted Service）**
`BackgroundService` を継承して、定期的に DB から祝日データを取得する Hosted Service を作成します。

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

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
```

---

### **3. `Scheduler` の修正**
`Scheduler` クラスは `HolidaysService` からデータを取得するようにします。

```csharp
public class Scheduler
{
    private readonly Lazy<SortedSet<DateTime>> holidays;
    private readonly DateTime _baseDate;

    public Scheduler(DateTime baseDate, IHolidaysProvider holidaysProvider)
    {
        _baseDate = baseDate;
        holidays = new Lazy<SortedSet<DateTime>>(holidaysProvider.GetHolidays);
    }

    private bool IsBusinessDay(DateTime date, TimeSpan offset)
    {
        DateTime adjustedDate = date - offset;
        return !IsWeekend(adjustedDate) && !holidays.Value.Contains(adjustedDate.Date);
    }
}
```

---

### **4. `Program.cs` で Hosted Service を登録**
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.SqlClient;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IDbConnection>(sp => new SqlConnection("YourConnectionString"));
        services.AddSingleton<IHolidaysProvider, HolidaysService>(); // 祝日データのプロバイダ
        services.AddSingleton<Scheduler>(); // `Scheduler` に `IHolidaysProvider` を注入
        services.AddHostedService<HolidaysService>(); // `HolidaysService` を Hosted Service として登録
    })
    .Build();

await host.RunAsync();
```

---

### **5. 仕組みの説明**
- `HolidaysService` は `BackgroundService` を継承し、1日ごとにデータベースから祝日を取得します。
- `IHolidaysProvider` を実装し、現在の祝日リストを `GetHolidays()` メソッドで提供します。
- `Scheduler` は `HolidaysService` を `IHolidaysProvider` として参照し、最新の祝日情報を取得します。
- `Program.cs` で `HolidaysService` を `AddHostedService()` で登録し、DI コンテナに注入します。

---

### **6. メリット**
✅ `Scheduler` は DB への直接アクセスなしに、常に最新の祝日情報を取得できる  
✅ `HolidaysService` はバックグラウンドで非同期実行され、システムの負担を軽減  
✅ `AddScoped()` ではなく `AddHostedService()` で管理されるため、永続的なデータ更新が可能  

この実装なら、`Scheduler` を使う側は特に変更なしで、DB から最新の祝日情報を利用できます！ 🚀