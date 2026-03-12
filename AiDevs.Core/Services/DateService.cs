namespace AiDevs.Core.Services;

public class DateService
{
    public static List<DateTime> GetNWorkingDaysFrom(DateTime startDate, int n)
    {
        var currentDate = startDate;
        var result = new List<DateTime>();
        while (result.Count < n)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                result.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }
        return result;;
    }
}
