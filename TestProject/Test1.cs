using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClassLibrary.Tests;

[TestClass]
public class SchedulerTests
{
    private DateTime baseDate;
    private Scheduler scheduler;

    [TestInitialize]
    public void Setup()
    {
        baseDate = new DateTime(2025, 3, 5, 10, 0, 0); // 2025年3月5日 10:00 (Wed)
        scheduler = new Scheduler(baseDate);
    }

    [TestMethod]
    public void GetNextExecutionTime_OnDemand_ReturnsBaseDate()
    {
        DateTime result = scheduler.GetNextExecutionTime("ONDEMAND", "", "");
        Assert.AreEqual(baseDate, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Daily_NonBusinessDay()
    {
        DateTime result = scheduler.GetNextExecutionTime("DAILY", "12:00:00|0", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Daily_BusinessDay()
    {
        DateTime result = scheduler.GetNextExecutionTime("DAILY", "12:00:00|1", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_NonBusinessDay()
    {
        DateTime result = scheduler.GetNextExecutionTime("WEEKLY", "12:00:00|0|135", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_BusinessDay()
    {
        DateTime result = scheduler.GetNextExecutionTime("WEEKLY", "12:00:00|1|135", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_NonBusinessDay2()
    {
        DateTime result = scheduler.GetNextExecutionTime("WEEKLY", "12:00:00|0|1", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetNextExecutionTime_Weekly_BusinessDay2()
    {
        DateTime result = scheduler.GetNextExecutionTime("WEEKLY", "12:00:00|1|1", "00:00:00");
        DateTime expected = baseDate.Date.AddHours(12);
        Assert.AreEqual(expected, result);
    }
}
