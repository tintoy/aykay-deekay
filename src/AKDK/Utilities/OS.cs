using System.Runtime.InteropServices;

namespace AKDK.Utilities
{
    /// <summary>
    ///     Basic information about the local operating system.
    /// </summary>
    static class OS
    {
        /// <summary>
        ///     Type initialiser for <see cref="OS"/>.
        /// </summary>
        static OS()
        {
            IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            IsUnix = IsMac || IsLinux;

            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Is64Bit = RuntimeInformation.OSArchitecture == Architecture.X64;
            Is64BitProcess = RuntimeInformation.ProcessArchitecture == Architecture.X64;
        }

        /// <summary>
        ///     Is the current operating system MacOS (OSX)?
        /// </summary>
        public static bool IsMac { get; private set; }

        /// <summary>
        ///     Is the current operating system Linux?
        /// </summary>
        public static bool IsLinux { get; private set; }

         /// <summary>
        ///     Is the current operating system a UNIX variant (i.e. Linux / MacOS)?
        /// </summary>
        public static bool IsUnix { get; private set; }

        /// <summary>
        ///     Is the current operating system Windows?
        /// </summary>
        public static bool IsWindows { get; private set; }

        /// <summary>
        ///     Is the current operating system 64-bit?
        /// </summary>
        public static bool Is64Bit { get; private set; }

        /// <summary>
        ///     Is the current process 64-bit?
        /// </summary>
        public static bool Is64BitProcess { get; private set; }
    }
}
