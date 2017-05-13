using System;

namespace AKDK.Utilities
{
    /// <summary>
    ///     Helper methods for working with <see cref="DateTime"/>s.
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        ///     The UNIX epoch (January 1st, 1970).
        /// </summary>
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        ///     Create a UTC <see cref="DateTime"/> representing the specified number of seconds after the UNIX epoch.
        /// </summary>
        /// <param name="seconds">
        ///     The number of seconds after the UNIX epoch.
        /// </param>
        /// <returns>
        ///     The new <see cref="DateTime"/>.
        /// </returns>
        public static DateTime FromUnixSecondsUTC(double seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }

        /// <summary>
        ///     Get the number of seconds after the UNIX epoch represented by the <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">
        ///     The <see cref="DateTime"/>.
        /// </param>
        /// <returns>
        ///     A <see cref="Double"/> representing the number of seconds after the UNIX epoch.
        /// </returns>
        public static double ToUnixSeconds(this DateTime dateTime)
        {
            return dateTime.Subtract(UnixEpoch).TotalSeconds;
        }

        /// <summary>
        ///     Create a UTC <see cref="DateTime"/> representing the specified number of nanoseconds after the UNIX epoch.
        /// </summary>
        /// <param name="nanoSeconds">
        ///     The number of nanoseconds after the UNIX epoch.
        /// </param>
        /// <returns>
        ///     The new <see cref="DateTime"/>.
        /// </returns>
        public static DateTime FromUnixNanosUTC(double nanoSeconds)
        {
            double milliseconds = nanoSeconds * 1000;

            return UnixEpoch.AddMilliseconds(milliseconds);
        }

        /// <summary>
        ///     Get the number of nanoseconds after the UNIX epoch represented by the <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">
        ///     The <see cref="DateTime"/>.
        /// </param>
        /// <returns>
        ///     A <see cref="Double"/> representing the number of nanoseconds after the UNIX epoch.
        /// </returns>
        public static double ToUnixNanos(this DateTime dateTime)
        {
            return dateTime.Subtract(UnixEpoch).TotalMilliseconds / 1000;
        }
    }
}

