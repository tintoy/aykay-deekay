using System;
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

        /// <summary>
        ///     Read all text from the specified file.
        /// </summary>
        /// <param name="file">
        ///     The file.
        /// </param>
        /// <returns>
        ///     The file content.
        /// </returns>
        public static string ReadAllText(this FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            return File.ReadAllText(file.FullName);
        }

        /// <summary>
        ///     Read all lines from the specified file.
        /// </summary>
        /// <param name="file">
        ///     The file.
        /// </param>
        /// <returns>
        ///     The file (as an array of lines).
        /// </returns>
        public static string[] ReadAllLines(this FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            return File.ReadAllLines(file.FullName);
        }
    }
}
