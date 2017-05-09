using Akka.Actor;
using Docker.DotNet;
using Docker.DotNet.Models;
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

            HandleDockerApiRequests();
            HandleDockerApiResponses();
            Receive<CommandResult>(commandResult =>
            {
                InFlightRequest inFlightRequest;
                if (!_inFlightRequests.TryGetValue(commandResult.CorrelationId, out inFlightRequest))
                {
                    Log.Warning("Received unexpected {0} response (CorrelationId = '{1}').",
                        commandResult.Response.GetType().Name,
                        commandResult.CorrelationId
                    );

                    Unhandled(commandResult);

                    return;
                }

                if (!commandResult.Success)
                {
                    Log.Info("{0} command '{1}' succeeded (response will be sent to '{2}').",
                        inFlightRequest.OperationName,
                        inFlightRequest.CorrelationId,
                        inFlightRequest.ReplyTo.Path
                    );

                    inFlightRequest.ReplyTo.Tell(commandResult.Response);
                }
                else
                {
                    Log.Info("{0} command '{1}' failed (error response will be sent to '{2}').",
                        inFlightRequest.OperationName,
                        inFlightRequest.CorrelationId,
                        inFlightRequest.ReplyTo.Path
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
        ///		Handle Docker API requests.
        /// </summary>
        void HandleDockerApiRequests()
        {
            Receive<ListImages>(listImages =>
            {
                InFlightRequest request = CreateRequest(listImages, replyTo: Sender);

                ListImages(request, listImages.Parameters)
                    .PipeTo(Self, failure: exception =>
                    {
                        return new ErrorResponse(listImages, exception);
                    });
            });
        }

        /// <summary>
        ///		Handle response messages resulting from Docker API calls.
        /// </summary>
        void HandleDockerApiResponses()
        {
            Receive<ImageList>(imageList =>
            {
                InFlightRequest request;
                if (!_inFlightRequests.TryGetValue(imageList.CorrelationId, out request))
                {
                    Log.Warning("Received unexpected ImageList response (CorrelationId = '{0}').", imageList.CorrelationId);

                    Unhandled(imageList);

                    return;
                }

                _inFlightRequests.Remove(imageList.CorrelationId);

                request.ReplyTo.Tell(imageList);
            });
            Receive<ErrorResponse>(errorResponse =>
            {
                InFlightRequest failedRequest;
                if (!_inFlightRequests.TryGetValue(errorResponse.CorrelationId, out failedRequest))
                {
                    Log.Warning("Received unexpected {0} response (CorrelationId = '{1}').",
                        errorResponse.Request.GetType().Name,
                        errorResponse.CorrelationId
                    );

                    Unhandled(errorResponse);

                    return;
                }

                Log.Error(errorResponse.Exception, "{0} request '{1}' failed: {2}",
                    failedRequest.OperationName,
                    errorResponse.CorrelationId,
                    errorResponse.Exception.Message
                );

                _inFlightRequests.Remove(errorResponse.CorrelationId);

                if (failedRequest.ResponseStreamer != null)
                    _responseStreamers.Remove(failedRequest.ResponseStreamer);

                string errorMessage = String.Format("{0} request '{1}' failed: {2}",
                    failedRequest.OperationName,
                    errorResponse.CorrelationId,
                    errorResponse.Exception.Message
                );
                failedRequest.ReplyTo.Tell(
                    new Failed(
                        correlationId: errorResponse.CorrelationId,
                        operationName: failedRequest.OperationName,
                        exception: new Exception(errorMessage, errorResponse.Exception)
                    )
                );
            });
            // TODO: Handle StreamLines.StreamLine.
            Receive<StreamLines.EndOfStream>(endOfStream =>
            {
                InFlightRequest streamingRequest;
                if (!_inFlightRequests.TryGetValue(endOfStream.CorrelationId, out streamingRequest))
                {
                    Unhandled(endOfStream);

                    return;
                }

                IActorRef responseStreamer = streamingRequest.ResponseStreamer;

                Log.Info("Response stream for request '{0}' is complete.", endOfStream.CorrelationId);

                _inFlightRequests.Remove(endOfStream.CorrelationId);
                _responseStreamers.Remove(streamingRequest.ResponseStreamer);
            });
        }

        /// <summary>
        ///		Retrieve a list of images from the Docker API.
        /// </summary>
        /// <param name="inFlightRequest">
        ///		A <see cref="Request"/> representing the request to list images.
        /// </param>
        /// <param name="parameters">
        ///		<see cref="ImagesListParameters"/> used to control operation behaviour.
        /// </param>
        /// <returns>
        ///		An <see cref="ImageList"/> containing the images.
        /// </returns>
        async Task<ImageList> ListImages(InFlightRequest inFlightRequest, ImagesListParameters parameters)
        {
            if (inFlightRequest == null)
                throw new ArgumentNullException(nameof(inFlightRequest));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            IList<ImagesListResponse> images = await _client.Images.ListImagesAsync(parameters);

            return new ImageList(inFlightRequest.CorrelationId, images);
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
        public static Props Create(DockerClient client)
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