using Akka.IO;
using Akka.Streams;
using Akka.Streams.Stage;
using System;
using System.Collections.Generic;

namespace AKDK.Actors
{
    using Messages;

    /// <summary>
    ///     A graph stage used to parse <see cref="DockerLogEntry"/> from <see cref="ByteString"/>.
    /// </summary>
    public class DockerLogParserStage
        : GraphStage<FlowShape<ByteString, DockerLogEntry>>
    {
        /// <summary>
        ///     Create a new <see cref="DockerLogParserStage"/>.
        /// </summary>
        /// <param name="correlationId">
        ///     An optional correlation Id to attach to each <see cref="DockerLogEntry"/>.
        /// </param>
        public DockerLogParserStage(string correlationId = null)
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        ///     The correlation Id to attach to each <see cref="DockerLogEntry"/>.
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        ///     The inlet for <see cref="ByteString"/>s to be parsed.
        /// </summary>
        public Inlet<ByteString> In { get; } = new Inlet<ByteString>("LogParser.In");

        /// <summary>
        ///     The outlet for each parsed <see cref="DockerLogEntry"/>.
        /// </summary>
        public Outlet<DockerLogEntry> Out { get; } = new Outlet<DockerLogEntry>("LogParser.Out");

        /// <summary>
        ///     The resulting flow shape.
        /// </summary>
        public override FlowShape<ByteString, DockerLogEntry> Shape => new FlowShape<ByteString, DockerLogEntry>(In, Out);

        /// <summary>
        ///     Create the <see cref="GraphStageLogic"/> that implements the graph stage.
        /// </summary>
        /// <param name="inheritedAttributes">
        ///     Inherited attributes (if any).
        /// </param>
        /// <returns>
        ///     The <see cref="GraphStageLogic"/>.
        /// </returns>
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        /// <summary>
        ///     The logic that implements the <see cref="DockerLogParserStage"/> graph stage.
        /// </summary>
        class Logic
            : InAndOutGraphStageLogic
        {
            /// <summary>
            ///     The implemented <see cref="DockerLogParserStage"/>.
            /// </summary>
            readonly DockerLogParserStage _stage;

            /// <summary>
            ///     The buffer representing accumulated data from upstream.
            /// </summary>
            ByteString _buffer = ByteString.Empty;

            /// <summary>
            ///     Create new <see cref="DockerLogParserStage"/> logic.
            /// </summary>
            /// <param name="stage">
            ///     The implemented <see cref="DockerLogParserStage"/>.
            /// </param>
            public Logic(DockerLogParserStage stage)
                : base(stage.Shape)
            {
                _stage = stage;

                SetHandler(_stage.In, this);
                SetHandler(_stage.Out, this);
            }

            /// <summary>
            ///     Called when data is pulled downstream to the <see cref="DockerLogParserStage"/>.
            /// </summary>
            public override void OnPull()
            {
                ParseLogEntries();
            }

            /// <summary>
            ///     Called when upstream has completed.
            /// </summary>
            public override void OnUpstreamFinish()
            {
                ParseLogEntries();
                Complete(_stage.Out);

                base.OnUpstreamFinish();
            }

            /// <summary>
            ///     Called when data is pushed upstream to the <see cref="DockerLogParserStage"/>.
            /// </summary>
            public override void OnPush()
            {
                _buffer += Grab(_stage.In);

                ParseLogEntries();
            }

            /// <summary>
            ///     Parse and emit any available log entries downstream.
            /// </summary>
            /// <param name="canPull">
            ///     If no log entries are available, request more data from upstream?
            /// </param>
            void ParseLogEntries(bool canPull = true)
            {
                List<DockerLogEntry> logEntries = new List<DockerLogEntry>();

                while (true)
                {
                    DockerLogEntry logEntry;
                    (logEntry, _buffer) = DockerLogEntry.ReadFrom(_buffer, _stage.CorrelationId);
                    if (logEntry == null)
                        break;
                    
                    logEntries.Add(logEntry);
                }

                if (logEntries.Count > 0)
                    EmitMultiple(_stage.Out, logEntries);
                else if (canPull)
                    PullData();
            }

            /// <summary>
            ///     Request more data from upstream.
            /// </summary>
            void PullData()
            {
                if (IsClosed(_stage.In))
                {
                    ParseLogEntries(canPull: false);

                    CompleteStage();
                }
                else
                    Pull(_stage.In);                
            }
        }
    }
}