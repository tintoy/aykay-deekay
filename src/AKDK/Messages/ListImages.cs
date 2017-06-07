using System.Collections.Generic;
using System.Collections.Immutable;

namespace AKDK.Messages
{
    using Utilities;

    /// <summary>
    ///		Request a list of images from the Docker API.
    /// </summary>
    public class ListImages
        : Request
    {
        /// <summary>
        ///		Create a new <see cref="ListImages"/> message.
        /// </summary>
        /// <param name="matchName">
        ///     An optional image name to match.
        /// </param>
        /// <param name="all">
        ///     Retrieve all images?
        /// </param>
        /// <param name="filters">
        ///     Optional image filters.
        /// </param>
        /// <param name="correlationId">
        ///		An optional message correlation Id (if not specified, a random value will be assigned to the request).
        /// </param>
        public ListImages(string matchName = null, bool? all = null, IDictionary<string, IDictionary<string, bool>> filters = null, string correlationId = null)
            : base(correlationId)
        {
            MatchName = matchName;
            All = all;
            
            if (filters != null)
                Filters = filters.ToImmutable();
        }

        /// <summary>
        ///     An optional image name to match.
        /// </summary>
        public string MatchName { get; set; }

        /// <summary>
        ///     Retrieve all images?
        /// </summary>
        public bool? All { get; set; }

        /// <summary>
        ///     Optional image filters.
        /// </summary>
        public ImmutableDictionary<string, ImmutableDictionary<string, bool>> Filters { get; } = ImmutableDictionary<string, ImmutableDictionary<string, bool>>.Empty;

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => "List Images";

    }
}
