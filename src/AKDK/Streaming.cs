using System;
using System.IO;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Actors;
using Akka.Streams.Dsl;
using Akka.Streams.IO;
using Reactive.Streams;

namespace AKDK
{
    /// <summary>
    ///     Helper functions for streaming data into actors.
    /// </summary>
    public static class Streaming
    {
        public static void Temp()
        {
            Source<ByteString, NotUsed> rawEventStream = Source.Empty<ByteString>();
            Source<string, NotUsed> lines = rawEventStream.SplitLines(
                maxLineLength: 1024 * 1024 // An event payload larger than 1MB has to be a mistake.
            );

            // TODO: Work out how to stream from System.IO.Stream.
        }

        /// <summary>
        ///     Split <see cref="ByteString"/>s into lines (as <see cref="String"/>s).
        /// </summary>
        /// <param name="source">
        ///     A source of <see cref="ByteString"/>s.
        /// </param>
        /// <param name="maxLineLength">
        ///     The maximum length of any individual line (a line longer than this will cause the source to fault).
        /// </param>
        /// <returns>
        ///     A source of lines (as strings).
        /// </returns>
        public static Source<string, NotUsed> SplitLines(this Source<ByteString, NotUsed> source, int maxLineLength = 1024)
        {
            Source<ByteString, NotUsed> lines = source.Via(Framing.Delimiter(
                delimiter: ByteString.FromString("\r\n"),
                maximumFrameLength: 1024 * 1024, // An event payload larger than 1MB has to be a mistake.
                allowTruncation: false
            ));

            return lines.Select(
                lineData => lineData.DecodeString()
            );
        }
    }
}