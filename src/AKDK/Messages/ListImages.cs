using Docker.DotNet.Models;
using System;

namespace AKDK.Messages
{
	/// <summary>
	///		Request a list of images from the Docker API.
	/// </summary>
    public class ListImages
		: Request
	{
		/// <summary>
		///		Create a new <see cref="ListImages"/> message.
		/// </summary>
		/// <param name="parameters">
		///		<see cref="ImagesListParameters"/> used to control operation behaviour.
		/// </param>
		/// <param name="correlationId">
		///		An optional message correlation Id (if not specified, a random value will be assigned to the request).
		/// </param>
		public ListImages(ImagesListParameters parameters, string correlationId = null)
			: base(correlationId)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			Parameters = parameters;
		}

		/// <summary>
		///		<see cref="ImagesListParameters"/> used to control operation behaviour.
		/// </summary>
		public ImagesListParameters Parameters { get; }

	}
}
