using System;

namespace AKDK.Messages
{
    /// <summary>
    ///     Well-known stream types for docker log entries.
    /// </summary>
    public enum DockerLogStreamType
        : Byte
    {
        /// <summary>
        ///     Standard input (STDIN).
        /// </summary>
        StdIn = 0,

        /// <summary>
        ///     Standard output (STDOUT).
        /// </summary>
        StdOut = 1,

        /// <summary>
        ///     Standard error (STDERR).
        /// </summary>
        StdErr = 2,

        /// <summary>
        ///     An unknown stream type.
        /// </summary>
        Unknown = 255
    }
}
