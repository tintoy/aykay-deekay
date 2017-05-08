using Akka.IO;
using System;
using System.Text;

namespace AKDK.Utilities
{
    /// <summary>
    ///		Extension methods for <see cref="ByteString"/>.
    /// </summary>
    public static class ByteStringExtensions
    {
        /// <summary>
        ///		Determine the index (if any) of a sub-string.
        /// </summary>
        /// <param name="data">
        ///		The data to search.
        /// </param>
        /// <param name="find">
        ///		The sub-string to find.
        /// </param>
        /// <returns>
        ///		The starting index of the substring within the <paramref name="data"/>, or -1 if the sub-string was not found.
        /// </returns>
        public static int IndexOf(this ByteString data, ByteString find)
        {
            int finalFindIndex = find.Count - 1;

            for (int index = 0; index < data.Count; index++)
            {
                for (int findIndex = 0; findIndex < find.Count; findIndex++)
                {
                    if (data[index + findIndex] != find[findIndex])
                        break;

                    if (findIndex == finalFindIndex)
                        return index;
                }
            }

            return -1;
        }

        /// <summary>
        ///		Extract part of the data as a string.
        /// </summary>
        /// <param name="data">
        ///		The data.
        /// </param>
        /// <param name="index">
        ///		The starting index.
        /// </param>
        /// <param name="count">
        ///		The number of bytes to extract.
        /// </param>
        /// <param name="encoding">
        ///		Optional <see cref="Encoding"/> to use (defaults to <see cref="Encoding.Unicode"/>).
        /// </param>
        /// <returns>
        ///		The sub-string.
        /// </returns>
        public static string Substring(this ByteString data, int index, int count, Encoding encoding = null)
        {
            return data.Slice(index, index + count).DecodeString(
                encoding ?? Encoding.Unicode
            );
        }
    }
}