using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AKDK.Messages
{
    /// <summary>
    ///		Represents a list of images from the Docker API.
    /// </summary>
    public class ImageList
        : Response
    {
        /// <summary>
        ///		Create a new <see cref="ImageList"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that was assigned to the original <see cref="ListImages"/> request.
        /// </param>
        /// <param name="images">
        ///		Information about the images.
        /// </param>
        public ImageList(string correlationId, IEnumerable<ImagesListResponse> images)
            : base(correlationId)
        {
            if (images == null)
                throw new ArgumentNullException(nameof(images));

            Images = ImmutableList.CreateRange(images);
        }

        /// <summary>
        ///		Information about the images.
        /// </summary>
        public ImmutableList<ImagesListResponse> Images { get; }
    }
}
