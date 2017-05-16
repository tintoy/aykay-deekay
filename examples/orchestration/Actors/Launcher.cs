using Akka.Actor;
using AKDK.Actors;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AKDK.Examples.Orchestration.Actors
{
    using Messages;

    // TODO: Need to stop relying on correlation Ids for job lookup (this is messy, implicit, and unreliable).
    //       Instead, either use job Id or sender's ActorRef.

    /// <summary>
    ///     Actor that launches and manages <see cref="Process"/>es.
    /// </summary>
    public partial class Launcher
        : ReceiveActorEx
    {
        /// <summary>
        ///     The default name for instances of the <see cref="Launcher"/> actor.
        /// </summary>
        public static readonly string ActorName = "launcher";

        /// <summary>
        ///     Processes, keyed by correlation Id.
        /// </summary>
        readonly Dictionary<string, ProcessInfo> _activeProcesses = new Dictionary<string, ProcessInfo>();

        /// <summary>
        ///     A reference to the <see cref="Client"/> actor for the docker API.
        /// </summary>
        readonly IActorRef _client;

        /// <summary>
        ///     Create a new <see cref="Launcher"/> actor.
        /// </summary>
        /// <param name="client">
        ///     A reference to the <see cref="Client"/> actor for the docker API.
        /// </param>
        public Launcher(IActorRef client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            _client = client;

            Receive<CreateProcess>(launch =>
            {
                var processInfo = new ProcessInfo
                {
                    RequestMessage = launch,
                    Owner = Sender
                };
                _activeProcesses.Add(processInfo.CorrelationId, processInfo);

                _client.Tell(new CreateContainer(
                    parameters: new CreateContainerParameters
                    {
                        Image = launch.ImageName,
                        Env = launch.EnvironmentVariables
                            .Select(
                                environmentVariable => $"{environmentVariable.Key}={environmentVariable.Value}"
                            )
                            .ToList(),
                        HostConfig = new HostConfig
                        {
                            Binds = launch.VolumeMounts
                                .Select(
                                    volumeMount => $"{volumeMount.Key}:{volumeMount.Value}"
                                )
                                .ToList(),
                            LogConfig = new LogConfig
                            {
                                Type = "json-file"
                            }
                        }
                    },
                    correlationId: processInfo.CorrelationId
                ));
            });
            Receive<ContainerCreated>(containerCreated =>
            {
                ProcessInfo processInfo = _activeProcesses[containerCreated.CorrelationId];
                processInfo.ContainerId = containerCreated.ContainerId;
                processInfo.Process = Context.ActorOf(
                    Process.Create(processInfo.Owner, _client, containerCreated.ContainerId),
                    name: containerCreated.ContainerId
                );
                Context.Watch(processInfo.Process);

                processInfo.Owner.Tell(
                    new ProcessCreated(processInfo.CorrelationId,
                        containerId: processInfo.ContainerId,
                        process: processInfo.Process
                    )
                );
            });
            Receive<ErrorResponse>(containerCreateFailed =>
            {
                ProcessInfo processInfo;
                if (!_activeProcesses.TryGetValue(containerCreateFailed.CorrelationId, out processInfo))
                {
                    Log.Warning("Received unexpected error response from '{0}' (CorrelationId = {1}).",
                        Sender,
                        containerCreateFailed.CorrelationId
                    );

                    Unhandled(containerCreateFailed);

                    return;
                }

                _activeProcesses.Remove(containerCreateFailed.CorrelationId);
            });
            Receive<Terminated>(terminated =>
            {
                ProcessInfo terminatedProcess = _activeProcesses.Values.FirstOrDefault(
                    processInfo => processInfo.Process != null && processInfo.Process == terminated.ActorRef
                );
                if (terminatedProcess == null)
                {
                    Log.Warning("Unexpected termination of watched actor '{0}'.", terminated.ActorRef);

                    Unhandled(terminated);

                    return;
                }

                _activeProcesses.Remove(terminatedProcess.CorrelationId);

                // TODO: Notify terminatedProcess.ReplyTo of process termination.
            });
        }

        /// <summary>
        ///     Information about a process.
        /// </summary>
        class ProcessInfo
        {
            /// <summary>
            ///     The message correlation Id from the request for which the process was created.
            /// </summary>
            public string CorrelationId => RequestMessage.CorrelationId;

            public CreateProcess RequestMessage { get; set; }

            public IActorRef Owner { get; set; }

            public string ContainerId { get; set; }

            public IActorRef Process { get; set; }
        }
    }
}
