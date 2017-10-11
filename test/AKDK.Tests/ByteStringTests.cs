using Akka.IO;
using System;
using System.Text;
using Xunit;

namespace AKDK.Tests
{
    using Utilities;

    public class ByteStringTests
    {
        /// <summary>
        ///     Find 2 bytes in 20 bytes at position 8.s
        /// </summary>
        [Fact]
        public void IndexOf_2_20_8()
        {
            const string data = "ABCDEFGHIJ";
            const string find = "E";
            const int expectedIndex = 8;
            Encoding encoding = Encoding.UTF8;

            ByteString dataBytes = ByteString.FromString(data, encoding);
            ByteString findBytes = ByteString.FromString(find, encoding);

            int actual = dataBytes.IndexOf(findBytes);

            Assert.Equal(expectedIndex, actual);
            Assert.Equal(find,
                dataBytes.Substring(expectedIndex, findBytes.Count)
            );
        }

        /// <summary>
        ///     Find 4 bytes in 30 bytes at position 20.
        /// </summary>
        [Fact]
        public void IndexOf_4_30_20()
        {
            const string data = "ABCDEFGHIJHELLO";
            const string find = "HE";
            const int expectedIndex = 20;
            Encoding encoding = Encoding.UTF8;

            ByteString dataBytes = ByteString.FromString(data, encoding);
            ByteString findBytes = ByteString.FromString(find, encoding);

            Console.WriteLine("FUU");

            int actual = dataBytes.IndexOf(findBytes);

            Assert.Equal(expectedIndex, actual);
            Assert.Equal(find,
                dataBytes.Substring(expectedIndex, findBytes.Count)
            );
        }
    }
}