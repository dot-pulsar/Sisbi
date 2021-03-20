using System;

namespace Sisbi.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTime(this DateTime dateTime)
        {
            return (long)dateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        public static DateTime ToDateTime(this long unixTime)
        {
            return new DateTime(DateTime.UnixEpoch.Ticks + (unixTime * TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        }
    }
}