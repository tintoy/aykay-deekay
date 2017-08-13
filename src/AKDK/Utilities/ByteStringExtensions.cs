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
                    if (index + findIndex >= data.Count)
                        break; // Not enough data left for a match.

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
            return data.Slice(index, count).ToString(
                encoding ?? Encoding.Unicode
            );
        }

        /// <summary>
        ///     Remove data from the start of the <see cref="ByteString"/>.
        /// </summary>
        /// <param name="data">
        ///     The <see cref="ByteString"/> to trim.
        /// </param>
        /// <param name="count">
        ///     The number of bytes to remove.
        /// </param>
        /// <returns>
        ///     The new <see cref="ByteString"/>.
        /// </returns>
        public static ByteString DropLeft(this ByteString data, int count) => data.Slice(0, count);

        /// <summary>
        ///     Remove data from the end of the <see cref="ByteString"/>.
        /// </summary>
        /// <param name="data">
        ///     The <see cref="ByteString"/> to trim.
        /// </param>
        /// <param name="count">
        ///     The number of bytes to remove.
        /// </param>
        /// <returns>
        ///     The new <see cref="ByteString"/>.
        /// </returns>
        public static ByteString DropRight(this ByteString data, int count) => data.Slice(data.Count - count - 1, count);

        /// <summary>
        ///     Split the <see cref="ByteString"/> at the specified index.
        /// </summary>
        /// <param name="data">
        ///     The <see cref="ByteString"/> to split.
        /// </param>
        /// <param name="index">
        ///     The target index.
        /// </param>
        /// <returns>
        ///     A <see cref="ValueTuple"/> containing the left and right <see cref="ByteString"/>s.
        /// </returns>
        public static (ByteString left, ByteString right) SplitAt(this ByteString data, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index cannot be less than 0.");

            if (index >= data.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index cannot be past the end of the ByteString.");

            return (
                left: data.Slice(0, index),
                right: data.Slice(index, data.Count - index)
            );
        }
    }
}