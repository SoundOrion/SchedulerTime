using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public interface IHolidaysProvider
{
    SortedSet<DateTime> GetHolidays();
}

public class DummyHolidaysProvider : IHolidaysProvider
{
    public SortedSet<DateTime> GetHolidays()
    {
        return
        [
            new DateTime(DateTime.Now.Year, 1, 1),
            new DateTime(DateTime.Now.Year, 3, 20),
            new DateTime(DateTime.Now.Year, 12, 25)
        ];
    }
}

public class DatabaseHolidaysProvider : IHolidaysProvider
{
    private readonly IDbConnection _dbConnection;

    public DatabaseHolidaysProvider(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public SortedSet<DateTime> GetHolidays()
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

        return holidaySet;
    }
}
