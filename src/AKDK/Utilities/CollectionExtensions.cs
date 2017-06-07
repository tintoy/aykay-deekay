using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AKDK.Utilities
{
    /// <summary>
    ///     Extension methods for collection types.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        ///     Convert a Docker API mutable filter-dictionary to its immutable equivalent.
        /// </summary>
        /// <param name="filters">
        ///     The mutable dictionary containing Docker-API-style filters.
        /// </param>
        /// <returns>
        ///     Nested immutable dictionaries representing the filters.
        /// </returns>
        public static ImmutableDictionary<string, ImmutableDictionary<string, string>> ToImmutable(this IDictionary<string, IDictionary<string, string>> filters)
        {
            if (filters == null)
                throw new ArgumentNullException(nameof(filters));

            var builder = ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty.ToBuilder();
            foreach (var keyValuePair in filters)
                builder[keyValuePair.Key] = ImmutableDictionary<string, string>.Empty.AddRange(keyValuePair.Value);

            return builder.ToImmutable();
        }

        /// <summary>
        ///     Convert a Docker API mutable filter-dictionary to its immutable equivalent.
        /// </summary>
        /// <param name="filters">
        ///     The mutable dictionary containing Docker-API-style filters.
        /// </param>
        /// <returns>
        ///     Nested immutable dictionaries representing the filters.
        /// </returns>
        public static ImmutableDictionary<string, ImmutableDictionary<string, bool>> ToImmutable(this IDictionary<string, IDictionary<string, bool>> filters)
        {
            if (filters == null)
                throw new ArgumentNullException(nameof(filters));

            var builder = ImmutableDictionary<string, ImmutableDictionary<string, bool>>.Empty.ToBuilder();
            foreach (var keyValuePair in filters)
                builder[keyValuePair.Key] = ImmutableDictionary<string, bool>.Empty.AddRange(keyValuePair.Value);

            return builder.ToImmutable();
        }

        /// <summary>
        ///     Convert a Docker API immutable filter-dictionary to its mutable equivalent.
        /// </summary>
        /// <param name="filters">
        ///     Nested immutable dictionaries representing the filters.
        /// </param>
        /// <returns>
        ///     The mutable dictionary containing Docker-API-style filters.
        /// </returns>
        public static IDictionary<string, IDictionary<string, string>> ToMutable(this ImmutableDictionary<string, ImmutableDictionary<string, string>> filters)
        {
            if (filters == null)
                throw new ArgumentNullException(nameof(filters));

            var mutableFilters = new Dictionary<string, IDictionary<string, string>>();
            foreach (var keyValuePair in filters)
                mutableFilters[keyValuePair.Key] = new Dictionary<string, string>(keyValuePair.Value);

            return mutableFilters;
        }

        /// <summary>
        ///     Convert a Docker API immutable filter-dictionary to its mutable equivalent.
        /// </summary>
        /// <param name="filters">
        ///     Nested immutable dictionaries representing the filters.
        /// </param>
        /// <returns>
        ///     The mutable dictionary containing Docker-API-style filters.
        /// </returns>
        public static IDictionary<string, IDictionary<string, bool>> ToMutable(this ImmutableDictionary<string, ImmutableDictionary<string, bool>> filters)
        {
            if (filters == null)
                throw new ArgumentNullException(nameof(filters));

            var mutableFilters = new Dictionary<string, IDictionary<string, bool>>();
            foreach (var keyValuePair in filters)
                mutableFilters[keyValuePair.Key] = new Dictionary<string, bool>(keyValuePair.Value);

            return mutableFilters;
        }
    }
}
