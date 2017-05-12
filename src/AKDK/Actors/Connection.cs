using Akka.Actor;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            _client = client;

            Become(Ready);
        }

        /// <summary>
        ///     Called when the connection is ready to handle requests.
        /// </summary>
        void Ready()
        {
            Receive<ExecuteCommand>(executeCommand =>
            {
                Log.Debug("Received ExecuteCommand '{0}' ('{1}') from '{2}'.",
                    executeCommand.CorrelationId,
                    executeCommand.RequestMessage.OperationName,
                    Sender.Path
                );
                Execute(executeCommand)
                    .PipeTo(Self, sender: Self, failure: exception =>
                    {
                        // Unwrap AggregateException if possible
                        if (exception is AggregateException aggregateException)
                        {
                            aggregateException = aggregateException.Flatten();
                            if (aggregateException.InnerExceptions.Count == 1)
                                exception = aggregateException.InnerExceptions[0];
                        }

                        Log.Error(exception, "ExecuteCommand '{0}' encountered an unhandled {1}; an ErrorResponse will be substituted.",
                            executeCommand.CorrelationId,
                            exception.GetType().FullName
                        );

                        return new ErrorResponse(executeCommand.RequestMessage, exception);
                    });                
            });
            Receive<CommandResult>(commandResult =>
            {
                Log.Debug("Received CommandResult '{0}' from '{1}'.",
                    commandResult.CorrelationId,
                    Sender.Path
                );

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
                    if (commandResult.IsStreamed)
                    {
                        switch (commandResult.Format)
                        {
                            case StreamedResponseFormat.Log:
                            {
                                Log.Debug("{0} command '{1}' succeeded (response will be streamed as log entries to '{2}').",
                                    inFlightRequest.OperationName,
                                    inFlightRequest.CorrelationId,
                                    inFlightRequest.ReplyTo.Path
                                );

                                StreamLogEntries(inFlightRequest, commandResult.ResponseStream);

                                break;
                            }
                            case StreamedResponseFormat.Events:
                            {
                                Log.Debug("{0} command '{1}' succeeded (response will be streamed to '{2}').",
                                    inFlightRequest.OperationName,
                                    inFlightRequest.CorrelationId,
                                    inFlightRequest.ReplyTo.Path
                                );

                                StreamEvents(inFlightRequest, commandResult.ResponseStream);

                                break;
                            }
                            default:
                            {
                                throw new InvalidOperationException($"Unrecognised stream format: '{commandResult.Format}'.");
                            }
                        }

                        return; // Command is still running.
                    }
                    else
                    {
                        Log.Debug("{0} command '{1}' succeeded (response will be sent to '{2}').",
                            inFlightRequest.OperationName,
                            inFlightRequest.CorrelationId,
                            inFlightRequest.ReplyTo.Path
                        );

                        inFlightRequest.ReplyTo.Tell(commandResult.ResponseMessage);
                    }
                }
                else
                {
                    Log.Warning("{0} command '{1}' failed (error response will be sent to '{2}'): {3}",
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
            Receive<CancelRequest>(cancelRequest =>
            {
                Log.Debug("Received CancelRequest '{0}' from '{1}'.",
                    cancelRequest.CorrelationId,
                    Sender.Path
                );

                InFlightRequest inFlightRequest;
                if (!_inFlightRequests.TryGetValue(cancelRequest.CorrelationId, out inFlightRequest))
                {
                    Log.Warning("Received unexpected cancellation request (CorrelationId = '{0}').",
                        cancelRequest.CorrelationId
                    );

                    Unhandled(cancelRequest);

                    return;
                }

                Log.Debug("Cancelling request '{0}' (originally initiated for '{1}').",
                    cancelRequest.CorrelationId,
                    inFlightRequest.ReplyTo
                );
                inFlightRequest.Cancel();

                if (inFlightRequest.ResponseStreamer != null)
                {
                    Context.Unwatch(inFlightRequest.ResponseStreamer);

                    inFlightRequest.ResponseStreamer.Tell(new ReadStream.Close(
                        correlationId: cancelRequest.CorrelationId
                    ));

                    _responseStreamers.Remove(inFlightRequest.ResponseStreamer);
                }

                _inFlightRequests.Remove(cancelRequest.CorrelationId);
            });
            Receive<Terminated>(terminated =>
            {
                InFlightRequest streamingRequest;
                if (!_responseStreamers.TryGetValue(terminated.ActorRef, out streamingRequest))
                {
                    Unhandled(terminated); // Will cause a DeathPactException.

                    return;
                }

                Log.Debug("Response streamer for request '{0}' ('{1}') has terminated.",
                    streamingRequest.CorrelationId,
                    terminated.ActorRef.Path
                );

                _inFlightRequests.Remove(streamingRequest.CorrelationId);
                _responseStreamers.Remove(streamingRequest.ResponseStreamer);
            });
            ReceiveSingleton<Close>(() =>
            {
                Log.Info("DockerConnection '{0}' received stop request from '{1}' - will terminate.",
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
            Log.Debug("Executing '{0}' command '{1}'.",
                request.RequestMessage.OperationName,
                request.CorrelationId
            );

            InFlightRequest inFlightRequest = CreateRequest(request.RequestMessage, replyTo: Sender);

            Response responseMessage = await request.Command(_client, inFlightRequest.Cancellation);

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
        ///		Pipe events from a Docker API response stream back to the requesting actor.
        /// </summary>
        /// <param name="inFlightRequest">
        ///		The in-flight request for which a response will be streamed.
        /// </param>
        /// <param name="stream">
        ///		The stream to read from.
        /// </param>
        /// <returns>
        ///		An <see cref="IActorRef"/> representing the actor that will perform the streaming.
        /// </returns>
        IActorRef StreamEvents(InFlightRequest inFlightRequest, Stream stream)
        {
            if (inFlightRequest == null)
                throw new ArgumentNullException(nameof(inFlightRequest));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            IActorRef responseStreamer = Context.ActorOf(
                DockerEventParser.Create(inFlightRequest.CorrelationId, inFlightRequest.ReplyTo, stream),
                name: $"event-stream-{inFlightRequest.CorrelationId}"
            );

            InFlightRequest streamingRequest = inFlightRequest.WithResponseStreamer(responseStreamer);

            _inFlightRequests[streamingRequest.CorrelationId] = streamingRequest;
            _responseStreamers.Add(responseStreamer, streamingRequest);

            Context.Watch(responseStreamer);

            return responseStreamer;
        }

        /// <summary>
        ///		Pipe log entries lines from a Docker API response stream back to the requesting actor.
        /// </summary>
        /// <param name="inFlightRequest">
        ///		The in-flight request for which a response will be streamed.
        /// </param>
        /// <param name="stream">
        ///		The stream to read from.
        /// </param>
        /// <returns>
        ///		An <see cref="IActorRef"/> representing the actor that will perform the streaming.
        /// </returns>
        IActorRef StreamLogEntries(InFlightRequest inFlightRequest, Stream stream)
        {
            if (inFlightRequest == null)
                throw new ArgumentNullException(nameof(inFlightRequest));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            IActorRef responseStreamer = Context.ActorOf(
                LogParser.Create(inFlightRequest.CorrelationId, inFlightRequest.ReplyTo, stream),
                name: $"log-stream-{inFlightRequest.CorrelationId}"
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
        sealed class InFlightRequest
        {
            /// <summary>
            ///     The cancellation token source for the request.
            /// </summary>
            readonly CancellationTokenSource _cancellationSource;

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
                : this(new CancellationTokenSource(), requestMessage, replyTo, responseStreamer: null)
            {
            }

            /// <summary>
            ///		Create a new <see cref="InFlightRequest"/> message.
            /// </summary>
            /// <param name="cancellationSource">
            ///     A source for cancellation tokens relating to the request.
            /// </param>
            /// <param name="requestMessage">
            ///		The request message that initiated the in-flight request.
            /// </param>
            /// <param name="replyTo">
            ///		The actor to which any response(s) will be sent.
            /// </param>
            /// <param name="responseStreamer">
            ///		The <see cref="StreamLines"/> actor that streams lines from the API response back to the <see cref="Connection"/>.
            /// </param>
            InFlightRequest(CancellationTokenSource cancellationSource, Request requestMessage, IActorRef replyTo, IActorRef responseStreamer)
            {
                if (cancellationSource == null)
                    throw new ArgumentNullException(nameof(cancellationSource));

                if (requestMessage == null)
                    throw new ArgumentNullException(nameof(requestMessage));

                if (replyTo == null)
                    throw new ArgumentNullException(nameof(replyTo));

                _cancellationSource = cancellationSource;
                RequestMessage = requestMessage;
                ReplyTo = replyTo;
                ResponseStreamer = responseStreamer;
            }

            /// <summary>
            ///     Dispose of resources being used by the in-flight request.
            /// </summary>
            public void Dispose()
            {
                _cancellationSource.Dispose();
            }

            /// <summary>
            ///     The cancellation token for the request.
            /// </summary>
            public CancellationToken Cancellation => _cancellationSource.Token;

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
            ///     Cancel the request (if possible) by signalling the request <see cref="Cancellation"/> token.
            /// </summary>
            public void Cancel()
            {
                _cancellationSource.Cancel();
            }

            /// <summary>
            ///     Cancel the request (if possible), after the specified delay, by signalling the request <see cref="Cancellation"/> token.
            /// </summary>
            /// <param name="delay">
            ///     The delay before the cancellation token is signaled.
            /// </param>
            public void CancelAfter(TimeSpan delay)
            {
                _cancellationSource.CancelAfter(delay);
            }

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
                
                return new InFlightRequest(_cancellationSource, RequestMessage, ReplyTo, responseStreamer);
            }

            /// <summary>
            ///     Create a copy of the <see cref="Request"/> without its <see cref="ResponseStreamer"/>.
            /// </summary>
            /// <returns>
            ///     The new <see cref="Request"/>.
            /// </returns>
            public InFlightRequest WithoutResponseStreamer()
            {
                return new InFlightRequest(_cancellationSource, RequestMessage, ReplyTo, responseStreamer: null);
            }
        }
    }
}