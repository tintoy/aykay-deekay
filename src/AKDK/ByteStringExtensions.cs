using Akka.IO;
using System;
using System.Text;

namespace AKDK
{
    public static class ByteStringExtensions
    {
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

		public static string Substring(this ByteString data, int index, int count, Encoding encoding = null)
        {
            return data.Slice(index, index + count).DecodeString(
                encoding ?? Encoding.Unicode
            );
        }
    }
}