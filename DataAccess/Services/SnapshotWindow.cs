namespace DataAccess.Services
{
    /// <summary>
    /// 每日快照涵蓋的時間範圍。
    /// 網站的作品、登入時間以伺服器時間（DateTime.Now）記錄，In 幣流水帳以 UTC 記錄，兩種都要換算成同一段時間。
    /// </summary>
    /// <param name="Date">日期</param>
    /// <param name="LocalStart">起始時間（含，伺服器時間）</param>
    /// <param name="LocalEnd">結束時間（不含，伺服器時間）</param>
    /// <param name="UtcStart">起始時間（含，UTC）</param>
    /// <param name="UtcEnd">結束時間（不含，UTC）</param>
    public sealed record SnapshotWindow(DateOnly Date, DateTime LocalStart, DateTime LocalEnd, DateTime UtcStart, DateTime UtcEnd)
    {
        /// <summary>
        /// 建立指定日期的時間範圍
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="timeZone">伺服器時區，預設為 <see cref="TimeZoneInfo.Local"/></param>
        /// <returns>時間範圍</returns>
        public static SnapshotWindow For(DateOnly date, TimeZoneInfo? timeZone = null)
        {
            timeZone ??= TimeZoneInfo.Local;
            var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var localEnd = localStart.AddDays(1);

            return new SnapshotWindow(
                date,
                localStart,
                localEnd,
                TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone),
                TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone));
        }

        /// <summary>
        /// 每日排程要寫入的日期：排程在午夜觸發，寫入剛結束的那一天
        /// </summary>
        /// <param name="localNow">目前的伺服器時間</param>
        /// <returns>前一天</returns>
        public static DateOnly PreviousDay(DateTime localNow)
        {
            return DateOnly.FromDateTime(localNow).AddDays(-1);
        }
    }
}
