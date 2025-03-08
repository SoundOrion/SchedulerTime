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
        int year = 2025;
        return
        [
            new DateTime(year, 1, 1),  // 元日
            new DateTime(year, 1, 13), // 成人の日（第2月曜日）
            new DateTime(year, 2, 11), // 建国記念の日
            new DateTime(year, 2, 23), // 天皇誕生日
            new DateTime(year, 3, 20), // 春分の日
            new DateTime(year, 4, 29), // 昭和の日
            new DateTime(year, 5, 3),  // 憲法記念日
            new DateTime(year, 5, 4),  // みどりの日
            new DateTime(year, 5, 5),  // こどもの日
            new DateTime(year, 5, 6),  // 振替休日（5月4日が日曜のため）
            new DateTime(year, 7, 21), // 海の日（第3月曜日）
            new DateTime(year, 8, 11), // 山の日
            new DateTime(year, 9, 15), // 敬老の日（第3月曜日）
            new DateTime(year, 9, 23), // 秋分の日
            new DateTime(year, 10, 13), // スポーツの日（第2月曜日）
            new DateTime(year, 11, 3),  // 文化の日
            new DateTime(year, 11, 23), // 勤労感謝の日
            new DateTime(year, 11, 24)  // 振替休日（11月23日が日曜のため）
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
