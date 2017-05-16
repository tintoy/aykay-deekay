using System;
using System.Collections.Generic;
using System.IO;

namespace AKDK.Examples.Orchestration.Utilities
{
    /// <summary>
    ///     Extensions for types from System.IO.
    /// </summary>
    public static class IOExtensions
    {
        /// <summary>
        ///     Get a <see cref="DirectoryInfo"/> representing the specified sub-directory.
        /// </summary>
        /// <param name="directory">
        ///     The parent directory.
        /// </param>
        /// <param name="relativePath">
        ///     The relative path of the sub-directory.
        /// </param>
        /// <returns>
        ///     The <see cref="DirectoryInfo"/>.
        /// </returns>
        public static DirectoryInfo GetSubDirectory(this DirectoryInfo directory, string relativePath)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            if (String.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(directory)}.", nameof(directory));

            return new DirectoryInfo(Path.Combine(
                directory.FullName, relativePath
            ));
        }

        /// <summary>
        ///     Get a <see cref="FileInfo"/> representing the specified sub-directory.
        /// </summary>
        /// <param name="directory">
        ///     The parent directory.
        /// </param>
        /// <param name="relativePath">
        ///     The relative path of the path.
        /// </param>
        /// <returns>
        ///     The <see cref="FileInfo"/>.
        /// </returns>
        public static FileInfo GetFile(this DirectoryInfo directory, string relativePath)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            if (String.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(directory)}.", nameof(directory));

            return new FileInfo(Path.Combine(
                directory.FullName, relativePath
            ));
        }
    }
}
