using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AKDK.Utilities
{
    /// <summary>
    ///     Helper functions for working with the local Docker instance.
    /// </summary>
    public static class LocalDocker
    {
        /// <summary>
        ///     The local end-point URI, on Windows, for the Docker API.
        /// </summary>
        static readonly Uri WindowsEndPointUri = new Uri("npipe://./pipe/docker_engine");

        /// <summary>
        ///     The local end-point URI, on MacOS, for the Docker API.
        /// </summary>
        static readonly Uri MacEndPointUri = new Uri("unix:///var/run/docker.sock");

        /// <summary>
        ///     The local end-point URI, on Linux, for the Docker API.
        /// </summary>
        static readonly Uri LinuxEndPointUri = new Uri("unix:///var/run/docker.sock");

        /// <summary>
        ///     Type initialiser.
        /// </summary>
        static LocalDocker()
        {
            if (OS.IsWindows)
                EndPointUri = WindowsEndPointUri;
            else if (OS.IsMac)
                EndPointUri = MacEndPointUri;
            else if (OS.IsLinux)
                EndPointUri = LinuxEndPointUri;
            else
                throw new PlatformNotSupportedException("Unable to determine the current platform.");
        }

        /// <summary>
        ///     The end-point URI for the local Docker API.
        /// </summary>
        public static Uri EndPointUri { get; }
    }
}
