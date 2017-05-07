using Akka.IO;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace AKDK.Tests
{
    public class ByteStringTests
    {
        [Fact]
        public void IndexOf_2_20_8() // Find 2 bytes in 20 bytes at position 8.
        {
            const string data = "ABCDEFGHIJ";
            const string find = "E";
            const int expectedIndex = 8;

            ByteString dataBytes = ByteString.FromString(data, Encoding.Unicode);
            ByteString findBytes = ByteString.FromString(find, Encoding.Unicode);

            int actual = dataBytes.IndexOf(findBytes);

            Assert.Equal(expectedIndex, actual);
            Assert.Equal(find,
                dataBytes.Substring(expectedIndex, findBytes.Count)
            );
        }

        [Fact]
        public void IndexOf_2_30_20() // Find 4 bytes in 30 bytes at position 20.
        {
            const string data = "ABCDEFGHIJHELLO";
            const string find = "HE";
            const int expectedIndex = 20;

            ByteString dataBytes = ByteString.FromString(data, Encoding.Unicode);
            ByteString findBytes = ByteString.FromString(find, Encoding.Unicode);

            int actual = dataBytes.IndexOf(findBytes);

            Assert.Equal(expectedIndex, actual);
            Assert.Equal(find,
                dataBytes.Substring(expectedIndex, findBytes.Count)
            );
        }
    }
}