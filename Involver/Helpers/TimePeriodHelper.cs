namespace Involver.Helpers
{
    public static class TimePeriodHelper
    {
        public static string Get(DateTime time)
        {
            DateTime now = DateTime.Now;

            TimeSpan period = now - time;

            decimal hours = (decimal)period.TotalHours;

            decimal days = period.Days;

            string periodString;

            if (hours > 24 -1)
            {
                if (days > 365 -1)
                {
                    decimal years = days / 365;

                    years = decimal.Round(years, MidpointRounding.AwayFromZero);

                    periodString = $"{years}年前";

                    return periodString;
                }

                if (days > 30 -1)
                {
                    decimal months = days / 30;

                    months = decimal.Round(months, MidpointRounding.AwayFromZero);

                    periodString = $"{months}個月前";

                    return periodString;
                }

                days = decimal.Round(days, MidpointRounding.AwayFromZero);

                periodString = $"{days}天前";

                return periodString;
            }
            else
            {
                if (hours > 1)
                {
                    hours = decimal.Round(hours, MidpointRounding.AwayFromZero);

                    periodString = $"{hours}小時前";

                    return periodString;
                }
                else
                {
                    decimal minutes = (decimal)period.TotalMinutes;

                    minutes = decimal.Round(minutes, MidpointRounding.AwayFromZero);

                    periodString = $"{minutes}分鐘前";

                    return periodString;
                }
            }
        }
    }
}
