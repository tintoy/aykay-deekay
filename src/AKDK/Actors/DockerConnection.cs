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
    ///     Represents a connection to the Docker API.
    /// </summary>
    public class DockerConnection
        : ReceiveActorEx
    {
        /// <summary>
        ///		Outstanding requests by name.
        /// </summary>
        readonly Dictionary<string, Request>    _outstandingRequests = new Dictionary<string, Request>();

        /// <summary>
        ///		Outstanding requests by response streamer.
        /// </summary>
        readonly Dictionary<IActorRef, Request> _responseStreamers = new Dictionary<IActorRef, Request>();

        /// <summary>
        ///     The underlying docker API client for the current connection.
        /// </summary>
        DockerClient _client;

        /// <summary>
        ///     Create a new <see cref="DockerConnection"/> actor.
        /// </summary>
        /// <param name="client">
        ///     The underlying docker API client.
        /// </param>
        public DockerConnection(DockerClient client)
        {
            _client = client;

            HandleDockerApiRequests();
            HandleDockerApiResponses();
            Receive<Terminated>(terminated =>
            {
                Request streamingRequest;
                if (!_responseStreamers.TryGetValue(terminated.ActorRef, out streamingRequest))
                {
                    Unhandled(terminated); // Will cause a DeathPactException.

                    return;
                }

                Log.Warning("Response streamer for request '{0}' ('{1}') terminated unexpectedly.",
                    streamingRequest.CorrelationId,
                    terminated.ActorRef.Path
                );

                _outstandingRequests.Remove(streamingRequest.CorrelationId);
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
                Request request = CreateRequest(listImages.CorrelationId, "List Images", replyTo: Sender);

                ListImages(request, listImages.Parameters)
                    .PipeTo(Self, failure: exception =>
                    {
                        return new ErrorResponse(request, exception);
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
                Request request;
                if (!_outstandingRequests.TryGetValue(imageList.CorrelationId, out request))
                {
                    Log.Warning("Received unexpected ImageList response (CorrelationId = '{0}').", imageList.CorrelationId);

                    Unhandled(imageList);

                    return;
                }

                _outstandingRequests.Remove(imageList.CorrelationId);

                request.ReplyTo.Tell(imageList);
            });
            Receive<ErrorResponse>(errorResponse =>
            {
                Log.Error(errorResponse.Exception, "{0} request '{1}' failed: {2}",
                    errorResponse.Request.OperationName,
                    errorResponse.CorrelationId,
                    errorResponse.Exception.Message
                );

                _outstandingRequests.Remove(errorResponse.CorrelationId);

                if (errorResponse.Request.ResponseStreamer != null)
                    _responseStreamers.Remove(errorResponse.Request.ResponseStreamer);

                string errorMessage = String.Format("{0} request '{1}' failed: {2}",
                    errorResponse.Request.OperationName,
                    errorResponse.CorrelationId,
                    errorResponse.Exception.Message
                );
                errorResponse.Request.ReplyTo.Tell(
                    new Failed(
                        correlationId: errorResponse.CorrelationId,
                        operationName: errorResponse.Request.OperationName,
                        exception: new Exception(errorMessage, errorResponse.Exception)
                    )
                );
            });
            // TODO: Handle StreamLines.StreamLine.
            Receive<StreamLines.EndOfStream>(endOfStream =>
            {
                Request streamingRequest;
                if (!_outstandingRequests.TryGetValue(endOfStream.CorrelationId, out streamingRequest))
                {
                    Unhandled(endOfStream);

                    return;
                }

                IActorRef responseStreamer = streamingRequest.ResponseStreamer;

                Log.Info("Response stream for request '{0}' is complete.", endOfStream.CorrelationId);

                _outstandingRequests.Remove(endOfStream.CorrelationId);
                _responseStreamers.Remove(streamingRequest.ResponseStreamer);
            });
        }

        /// <summary>
        ///		Retrieve a list of images from the Docker API.
        /// </summary>
        /// <param name="request">
        ///		A <see cref="Request"/> representing the request to list images.
        /// </param>
        /// <param name="parameters">
        ///		<see cref="ImagesListParameters"/> used to control operation behaviour.
        /// </param>
        /// <returns>
        ///		An <see cref="ImageList"/> containing the images.
        /// </returns>
        async Task<ImageList> ListImages(Request request, ImagesListParameters parameters)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            IList<ImagesListResponse> images = await _client.Images.ListImagesAsync(parameters);

            return new ImageList(request.CorrelationId, images);
        }

        /// <summary>
        ///		Create a new <see cref="Request"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The request correlation Id. If null or empty, a new GUID will be used.
        /// </param>
        /// <param name="operationName">
        ///		The name of the requested operation.
        /// </param>
        /// <param name="replyTo">
        ///		The actor to which any response(s) will be sent.
        /// </param>
        /// <returns>
        ///		The new <see cref="Request"/>.
        /// </returns>
        Request CreateRequest(string correlationId, string operationName, IActorRef replyTo)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(operationName)}.", nameof(operationName));

            if (replyTo == null)
                throw new ArgumentNullException(nameof(replyTo));

            if (String.IsNullOrWhiteSpace(correlationId))
                correlationId = Guid.NewGuid().ToString();

            if (_outstandingRequests.ContainsKey(correlationId))
                throw new InvalidOperationException($"There is already a request with correlation Id '{correlationId}'.");

            Request request = new Request(correlationId, operationName, replyTo);
            _outstandingRequests.Add(request.CorrelationId, request);

            return request;
        }

        /// <summary>
        ///		Pipe lines from a Docker API response stream back to the <see cref="DockerConnection"/>.
        /// </summary>
        /// <param name="stream">
        ///		The stream to read from.
        /// </param>
        /// <param name="request">
        ///		The request for which a response will be streamed.
        /// </param>
        /// <returns>
        ///		An <see cref="IActorRef"/> representing the actor that will perform the streaming.
        /// </returns>
        IActorRef StreamResponseLines(Stream stream, Request request)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            IActorRef responseStreamer = Context.ActorOf(
                StreamLines.Create(request.CorrelationId, Self, stream)
            );

            Request streamingRequest = request.WithResponseStreamer(responseStreamer);

            _outstandingRequests[streamingRequest.CorrelationId] = streamingRequest;
            _responseStreamers.Add(responseStreamer, streamingRequest);

            Context.Watch(responseStreamer);

            return responseStreamer;
        }

        /// <summary>
        ///     Build <see cref="Props"/> to create a <see cref="DockerConnection"/> actor.
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
                () => new DockerConnection(client)
            );
        }

        /// <summary>
        ///		Represents an outstanding request to the Docker API.
        /// </summary>
        class Request
        {
            /// <summary>
            ///		Create a new <see cref="Request"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///		An optional correlation Id that will be returned with the response.
            /// </param>
            /// <param name="operationName">
            ///		The name of the requested operation.
            /// </param>
            /// <param name="replyTo">
            ///		The actor to which any response(s) will be sent.
            /// </param>
            public Request(string correlationId, string operationName, IActorRef replyTo)
                : this(correlationId, operationName, replyTo, responseStreamer: null)
            {
            }

            /// <summary>
            ///		Create a new <see cref="Request"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///		An optional correlation Id that will be returned with the response.
            /// </param>
            /// <param name="operationName">
            ///		The name of the requested operation.
            /// </param>
            /// <param name="replyTo">
            ///		The actor to which any response(s) will be sent.
            /// </param>
            /// <param name="responseStreamer">
            ///		The <see cref="StreamLines"/> actor that streams lines from the API response back to the <see cref="DockerConnection"/>.
            /// </param>
            Request(string correlationId, string operationName, IActorRef replyTo, IActorRef responseStreamer)
            {
                CorrelationId = correlationId;
                OperationName = operationName;
                ReplyTo = replyTo;
                ResponseStreamer = responseStreamer;
            }

            public string CorrelationId { get; }

            public string OperationName { get; }

            public IActorRef ReplyTo { get; }

            public IActorRef ResponseStreamer { get; }

            public Request WithResponseStreamer(IActorRef responseStreamer)
            {
                return new Request(CorrelationId, OperationName, ReplyTo, responseStreamer);
            }
        }

        /// <summary>
        ///		Represents an error response from the Docker API.
        /// </summary>
        class ErrorResponse
            : CorrelatedMessage
        {
            /// <summary>
            ///		Create a new <see cref="ErrorResponse"/> message.
            /// </summary>
            /// <param name="request">
            ///		The request that the response relates to.
            /// </param>
            /// <param name="exception">
            ///		An exception representing the error.
            /// </param>
            public ErrorResponse(Request request, Exception exception)
                : base(request.CorrelationId)
            {
                Request = request;
                Exception = exception;
            }

            /// <summary>
            ///		The request that the response relates to.
            /// </summary>
            public Request Request { get; }

            /// <summary>
            ///		An exception representing the error.
            /// </summary>
            public Exception Exception { get; }
        }

        /// <summary>
        ///     Request to a <see cref="DockerConnection"/> requesting close of the underlying connection to the Docker API.
        /// </summary>
        public class Close
        {
            /// <summary>
            ///		The singleton instance of the <see cref="Close"/> message.
            /// </summary>
            public static readonly Close Instance = new Close();

            /// <summary>
            ///		Create a new <see cref="Close"/> message.
            /// </summary>
            Close()
            {
            }
        }
    }
}