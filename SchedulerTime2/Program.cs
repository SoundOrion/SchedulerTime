﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data;
using Microsoft.Data.SqlClient;
using ClassLibrary;
using Microsoft.Extensions.Configuration;

var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
            //config.AddEnvironmentVariables();
        })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

        services.AddSingleton<IDbConnection>(sp => new SqlConnection(connectionString));
        services.AddSingleton<IHolidaysProvider, DummyHolidaysProvider>(); // 祝日プロバイダ
        services.AddSingleton<Scheduler>(); // `Scheduler` に `IHolidaysProvider` を注入
        services.AddHostedService<HolidaysService>(); // `IHostedService` として登録
    })
    .Build();

await host.RunAsync();
