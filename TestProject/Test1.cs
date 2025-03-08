using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClassLibrary.Tests;

[TestClass]
public class SchedulerTests
{
    private Scheduler scheduler;

    [TestInitialize]
    public void Setup()
    {
        IHolidaysProvider holidaysProvider = new DummyHolidaysProvider();
        scheduler = new Scheduler(holidaysProvider);
    }

    [TestMethod]
    public void GetNextExecutionTime_OnDemand_ReturnsBaseDate()
    {
        var baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        DateTime result = scheduler.GetNextExecutionTime(baseDate, "ONDEMAND", "", "");
        Assert.AreEqual(baseDate, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Daily_NonBusinessDay()
    {
        var baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        DateTime result = scheduler.GetNextExecutionTime(baseDate, "DAILY", "12:00:00|0", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Daily_BusinessDay()
    {
        var baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        DateTime result = scheduler.GetNextExecutionTime(baseDate, "DAILY", "12:00:00|1", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_NonBusinessDay()
    {
        var baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        DateTime result = scheduler.GetNextExecutionTime(baseDate, "WEEKLY", "12:00:00|0|135", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_BusinessDay()
    {
        var baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        DateTime result = scheduler.GetNextExecutionTime(baseDate, "WEEKLY", "12:00:00|1|135", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_NonBusinessDay2()
    {
        var baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        DateTime result = scheduler.GetNextExecutionTime(baseDate, "WEEKLY", "12:00:00|0|1", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_BusinessDay2()
    {
        var baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        DateTime result = scheduler.GetNextExecutionTime(baseDate, "WEEKLY", "12:00:00|1|1", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }
}
