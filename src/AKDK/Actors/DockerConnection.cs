using Akka.Actor;
using Docker.DotNet;
using System;
using System.Collections.Generic;
using System.IO;

namespace AKDK.Actors
{
	using Actors.Streaming;

	/// <summary>
	///     Represents a connection to the Docker API.
	/// </summary>
	public class DockerConnection
        : ReceiveActorEx
    {
		/// <summary>
		///		Outstanding requests by name.
		/// </summary>
		readonly Dictionary<string, Request>	_outstandingRequests = new Dictionary<string, Request>();

		/// <summary>
		///		Outstanding requests by response streamer.
		/// </summary>
		readonly Dictionary<IActorRef, Request>	_responseStreamers = new Dictionary<IActorRef, Request>();

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

			Receive<StreamLines.EndOfStream>(endOfStream =>
			{
				Request outstandingRequest;
				if (!_outstandingRequests.TryGetValue(endOfStream.Name, out outstandingRequest))
				{
					Unhandled(endOfStream);

					return;
				}

				IActorRef responseStreamer = outstandingRequest.ResponseStreamer;

				Log.Info("Response stream for request '{0}' is complete.", endOfStream.Name);

				_outstandingRequests.Remove(endOfStream.Name);
				_responseStreamers.Remove(outstandingRequest.ResponseStreamer);
			});
			Receive<Terminated>(terminated =>
			{
				Request outstandingRequest;
				if (!_responseStreamers.TryGetValue(terminated.ActorRef, out outstandingRequest))
				{
					Unhandled(terminated); // Will cause a DeathPactException.

					return;
				}

				Log.Warning("Response streamer for request '{0}' ('{1}') terminated unexpectedly.",
					outstandingRequest.Name,
					terminated.ActorRef.Path
				);

				_outstandingRequests.Remove(outstandingRequest.Name);
				_responseStreamers.Remove(outstandingRequest.ResponseStreamer);
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
		///		Read lines from a Docker API response stream.
		/// </summary>
		/// <param name="stream">
		///		The stream to read from.
		/// </param>
		/// <param name="name">
		///		The request name (used to correlate stream data with the original request).
		/// </param>
		/// <returns>
		///		An <see cref="IActorRef"/> representing the actor that will perform the streaming.
		/// </returns>
		IActorRef StreamResponseLines(Stream stream, string name)
		{
			if (_outstandingRequests.ContainsKey(name))
				throw new InvalidOperationException($"There is already a request named '{name}'.");

			IActorRef responseStreamer = Context.ActorOf(
				StreamLines.Create(name, Self, stream)
			);

			Request outstandingRequest = new Request(name, responseStreamer);

			_outstandingRequests.Add(outstandingRequest.Name, outstandingRequest);
			_responseStreamers.Add(responseStreamer, outstandingRequest);

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
			public Request(string name, IActorRef responseStreamer)
			{
				Name = name;
				ResponseStreamer = responseStreamer;
			}

			public string Name { get; }

			public IActorRef ResponseStreamer { get; }
		}

        /// <summary>
        ///     Request to a <see cref="DockerConnection"/> requesting close of the underlying connection to the Docker API.
        /// </summary>
        public class Close
        {
            public static readonly Close Instance = new Close();

            Close() { }
        }
    }
}