using Akka.Actor;
using Docker.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AKDK.Actors
{
    using Actors.Streaming;
    using Messages;

    /// <summary>
    ///     Actor that manages a connection to the Docker API.
    /// </summary>
    public partial class Connection
        : ReceiveActorEx
    {
        /// <summary>
        ///		Outstanding requests by name.
        /// </summary>
        readonly Dictionary<string, InFlightRequest>    _inFlightRequests = new Dictionary<string, InFlightRequest>();

        /// <summary>
        ///		Outstanding requests by response streamer.
        /// </summary>
        readonly Dictionary<IActorRef, InFlightRequest> _responseStreamers = new Dictionary<IActorRef, InFlightRequest>();

        /// <summary>
        ///     The underlying docker API client for the current connection.
        /// </summary>
        IDockerClient _client;

        /// <summary>
        ///     Create a new <see cref="Connection"/> actor.
        /// </summary>
        /// <param name="client">
        ///     The underlying docker API client.
        /// </param>
        public Connection(IDockerClient client)
        {
            _client = client;

            Receive<ExecuteCommand>(executeCommand =>
            {
                Execute(executeCommand)
                    .PipeTo(Self, failure: exception =>
                    {
                        return new ErrorResponse(executeCommand.RequestMessage, exception);
                    });
            });
            Receive<CommandResult>(commandResult =>
            {
                InFlightRequest inFlightRequest;
                if (!_inFlightRequests.TryGetValue(commandResult.CorrelationId, out inFlightRequest))
                {
                    Log.Warning("Received unexpected {0} command result (CorrelationId = '{1}').",
                        commandResult.ResponseMessage.GetType().Name,
                        commandResult.CorrelationId
                    );

                    Unhandled(commandResult);

                    return;
                }

                if (commandResult.Success)
                {
                    Log.Info("{0} command '{1}' succeeded (response will be sent to '{2}').",
                        inFlightRequest.OperationName,
                        inFlightRequest.CorrelationId,
                        inFlightRequest.ReplyTo.Path
                    );

                    inFlightRequest.ReplyTo.Tell(commandResult.ResponseMessage);
                }
                else
                {
                    Log.Info("{0} command '{1}' failed (error response will be sent to '{2}'): {3}",
                        inFlightRequest.OperationName,
                        inFlightRequest.CorrelationId,
                        inFlightRequest.ReplyTo.Path,
                        commandResult.Exception.Message
                    );

                    inFlightRequest.ReplyTo.Tell(new ErrorResponse(
                        request: inFlightRequest.RequestMessage,
                        exception: commandResult.Exception
                    ));
                }

                _inFlightRequests.Remove(commandResult.CorrelationId);
            });
            Receive<Terminated>(terminated =>
            {
                InFlightRequest streamingRequest;
                if (!_responseStreamers.TryGetValue(terminated.ActorRef, out streamingRequest))
                {
                    Unhandled(terminated); // Will cause a DeathPactException.

                    return;
                }

                Log.Warning("Response streamer for request '{0}' ('{1}') terminated unexpectedly.",
                    streamingRequest.CorrelationId,
                    terminated.ActorRef.Path
                );

                _inFlightRequests.Remove(streamingRequest.CorrelationId);
                _responseStreamers.Remove(streamingRequest.ResponseStreamer);
            });
            ReceiveSingleton<Close>(() =>
            {
                Log.Info("DockerConnection '{0}' Received stop request from '{1}' - will terminate.",
                    Self.Path.Name, Sender
                );

                Context.Stop(Self);
            });
        }

        /// <summary>
        ///     Called when the actor is stopping.
        /// </summary>
        protected override void PostStop()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }

            base.PostStop();
        }
        
        /// <summary>
        ///     Execute a command.
        /// </summary>
        /// <param name="request">
        ///     An <see cref="ExecuteCommand"/> message indicating the command to execute.
        /// </param>
        /// <returns>
        ///     The command result.
        /// </returns>
        async Task<CommandResult> Execute(ExecuteCommand request)
        {
            CreateRequest(request.RequestMessage, replyTo: Sender);

            Response responseMessage = await request.Command(_client);

            return new CommandResult(responseMessage);
        }

        /// <summary>
        ///		Create a new <see cref="InFlightRequest"/>.
        /// </summary>
        /// <param name="requestMessage">
        ///		The message that initiated the request.
        /// </param>
        /// <param name="replyTo">
        ///		The actor to which any response(s) will be sent.
        /// </param>
        /// <returns>
        ///		The new <see cref="Request"/>.
        /// </returns>
        InFlightRequest CreateRequest(Request requestMessage, IActorRef replyTo)
        {
            if (replyTo == null)
                throw new ArgumentNullException(nameof(replyTo));

            if (_inFlightRequests.ContainsKey(requestMessage.CorrelationId))
                throw new InvalidOperationException($"There is already a request with correlation Id '{requestMessage.CorrelationId}'.");

            InFlightRequest request = new InFlightRequest(requestMessage, replyTo);
            _inFlightRequests.Add(request.CorrelationId, request);

            return request;
        }

        /// <summary>
        ///		Pipe lines from a Docker API response stream back to the <see cref="Connection"/>.
        /// </summary>
        /// <param name="stream">
        ///		The stream to read from.
        /// </param>
        /// <param name="inFlightRequest">
        ///		The in-flight request for which a response will be streamed.
        /// </param>
        /// <returns>
        ///		An <see cref="IActorRef"/> representing the actor that will perform the streaming.
        /// </returns>
        IActorRef StreamResponseLines(Stream stream, InFlightRequest inFlightRequest)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (inFlightRequest == null)
                throw new ArgumentNullException(nameof(inFlightRequest));

            IActorRef responseStreamer = Context.ActorOf(
                StreamLines.Create(inFlightRequest.CorrelationId, Self, stream)
            );

            InFlightRequest streamingRequest = inFlightRequest.WithResponseStreamer(responseStreamer);

            _inFlightRequests[streamingRequest.CorrelationId] = streamingRequest;
            _responseStreamers.Add(responseStreamer, streamingRequest);

            Context.Watch(responseStreamer);

            return responseStreamer;
        }

        /// <summary>
        ///     Build <see cref="Props"/> to create a <see cref="Connection"/> actor.
        /// </summary>
        /// <param name="client">
        ///     The underlying docker API client for the current connection.
        /// </param>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public static Props Create(IDockerClient client)
        {
            return Props.Create(
                () => new Connection(client)
            );
        }

        /// <summary>
        ///		Represents an in-flight request to the Docker API.
        /// </summary>
        class InFlightRequest
        {
            /// <summary>
            ///		Create a new <see cref="InFlightRequest"/> message.
            /// </summary>
            /// <param name="requestMessage">
            ///		The request message that initiated the in-flight request.
            /// </param>
            /// <param name="replyTo">
            ///		The actor to which any response(s) will be sent.
            /// </param>
            public InFlightRequest(Request requestMessage, IActorRef replyTo)
                : this(requestMessage, replyTo, responseStreamer: null)
            {
            }

            /// <summary>
            ///		Create a new <see cref="InFlightRequest"/> message.
            /// </summary>
            /// <param name="requestMessage">
            ///		The request message that initiated the in-flight request.
            /// </param>
            /// <param name="replyTo">
            ///		The actor to which any response(s) will be sent.
            /// </param>
            /// <param name="responseStreamer">
            ///		The <see cref="StreamLines"/> actor that streams lines from the API response back to the <see cref="Connection"/>.
            /// </param>
            InFlightRequest(Request requestMessage, IActorRef replyTo, IActorRef responseStreamer)
            {
                if (requestMessage == null)
                    throw new ArgumentNullException(nameof(requestMessage));

                RequestMessage = requestMessage;
                ReplyTo = replyTo;
                ResponseStreamer = responseStreamer;
            }

            /// <summary>
            ///     The request message that initiated the in-flight request.
            /// </summary>
            public Request RequestMessage { get; }

            /// <summary>
            ///     The message correlation Id that will be returned with the response.
            /// </summary>
            public string CorrelationId => RequestMessage.CorrelationId;

            /// <summary>
            ///     The name of the requested operation.
            /// </summary>
            public string OperationName => RequestMessage.OperationName;

            /// <summary>
            ///     The actor to which any response(s) will be sent.
            /// </summary>
            public IActorRef ReplyTo { get; }

            /// <summary>
            ///     The actor that streams the request's response data.
            /// </summary>
            public IActorRef ResponseStreamer { get; }

            /// <summary>
            ///     Create a copy of the <see cref="Request"/> with the specified <see cref="ResponseStreamer"/>.
            /// </summary>
            /// <param name="responseStreamer">
            ///     The actor that streams the request's response data.
            /// </param>
            /// <returns>
            ///     The new <see cref="Request"/>.
            /// </returns>
            public InFlightRequest WithResponseStreamer(IActorRef responseStreamer)
            {
                if (responseStreamer == null)
                    throw new ArgumentNullException(nameof(responseStreamer));
                
                return new InFlightRequest(RequestMessage, ReplyTo, responseStreamer);
            }

            /// <summary>
            ///     Create a copy of the <see cref="Request"/> without its <see cref="ResponseStreamer"/>.
            /// </summary>
            /// <returns>
            ///     The new <see cref="Request"/>.
            /// </returns>
            public InFlightRequest WithoutResponseStreamer()
            {
                return new InFlightRequest(RequestMessage, ReplyTo, responseStreamer: null);
            }
        }
    }
}