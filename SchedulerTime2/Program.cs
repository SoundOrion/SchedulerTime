using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data;
using Microsoft.Data.SqlClient;
using ClassLibrary;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IDbConnection>(sp => new SqlConnection("YourConnectionString"));
        services.AddSingleton<IHolidaysProvider, DummyHolidaysProvider>(); // 祝日プロバイダ
        services.AddSingleton<Scheduler>(); // `Scheduler` に `IHolidaysProvider` を注入
        services.AddHostedService<HolidaysService>(); // `IHostedService` として登録
    })
    .Build();

await host.RunAsync();
