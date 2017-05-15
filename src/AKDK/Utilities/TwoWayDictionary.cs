using System;
using System.Collections;
using System.Collections.Generic;

namespace AKDK.Utilities
{
    /// <summary>
    ///     A directionary that supports 2-way lookups.
    /// </summary>
    public class TwoWayDictionary<T1, T2>
    {
        /// <summary>
        ///     Dictionary to support forward-lookup.
        /// </summary>
        readonly Dictionary<T1, T2> _forwardDictionary = new Dictionary<T1, T2>();

        /// <summary>
        ///     Dictionary to support reverse-lookup.
        /// </summary>
        readonly Dictionary<T2, T1> _reverseDictionary = new Dictionary<T2, T1>();

        /// <summary>
        ///     Create a new <see cref="TwoWayDictionary{T1, T2}"/>.
        /// </summary>
        public TwoWayDictionary()
        {
            Forward = new DictionariesWrapper<T1, T2>(_forwardDictionary, _reverseDictionary);
            Reverse = new DictionariesWrapper<T2, T1>(_reverseDictionary, _forwardDictionary);
        }

        /// <summary>
        ///     Create a new <see cref="TwoWayDictionary{T1, T2}"/> by copying the items in the specified <see cref="TwoWayDictionary{T1, T2}"/>.
        /// </summary>
        /// <param name="twoWayDictionary">
        ///     The <see cref="TwoWayDictionary{T1, T2}"/> to copy.
        /// </param>
        public TwoWayDictionary(TwoWayDictionary<T1, T2> twoWayDictionary)
        {
            if (twoWayDictionary == null)
                throw new ArgumentNullException(nameof(twoWayDictionary));

            foreach (var item in twoWayDictionary._forwardDictionary)
                _forwardDictionary.Add(item.Key, item.Value);

            foreach (var item in twoWayDictionary._reverseDictionary)
                _reverseDictionary.Add(item.Key, item.Value);
        }

        /// <summary>
        ///     Create a new <see cref="TwoWayDictionary{T1, T2}"/> containing the specified items.
        /// </summary>
        /// <param name="items">
        ///     A sequence of <see cref="KeyValuePair{TKey, TValue}"/>s to add to the <see cref="TwoWayDictionary{T1, T2}"/>.
        /// </param>
        public TwoWayDictionary(IEnumerable<KeyValuePair<T2, T1>> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (KeyValuePair<T2, T1> item in items)
                Add(item.Key, item.Value);
        }

        /// <summary>
        ///     Create a new <see cref="TwoWayDictionary{T1, T2}"/> containing the specified items.
        /// </summary>
        /// <param name="items">
        ///     A sequence of <see cref="KeyValuePair{TKey, TValue}"/>s to add to the <see cref="TwoWayDictionary{T1, T2}"/>.
        /// </param>
        public TwoWayDictionary(IEnumerable<KeyValuePair<T1, T2>> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (KeyValuePair<T1, T2> item in items)
                Add(item.Key, item.Value);
        }

        /// <summary>
        ///     Dictionary access from <typeparamref name="T1"/> to <typeparamref name="T2"/>.
        /// </summary>
        public IDictionary<T1, T2> Forward { get; }

        /// <summary>
        ///     Dictionary access from <typeparamref name="T2"/> to <typeparamref name="T1"/>.
        /// </summary>
        public IDictionary<T2, T1> Reverse { get; }

        /// <summary>
        ///     Get or set the specified value.
        /// </summary>
        /// <param name="key">
        ///     The value's associated key.
        /// </param>
        public T2 this[T1 key]
        {
            get
            {
                return Forward[key];
            }
            set
            {
                Forward[key] = value;
            }
        }

        /// <summary>
        ///     Get or set the specified value.
        /// </summary>
        /// <param name="key">
        ///     The value's associated key.
        /// </param>
        public T1 this[T2 key]
        {
            get
            {
                return Reverse[key];
            }
            set
            {
                Reverse[key] = value;
            }
        }

        /// <summary>
        ///     Attempt to retrieve the value with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="value">
        ///     Receives the value.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the value pair retrieved; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetValue(T1 key, out T2 value) => Forward.TryGetValue(key, out value);

        /// <summary>
        ///     Attempt to retrieve the value with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="value">
        ///     Receives the value.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the value pair retrieved; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetValue(T2 key, out T1 value) => Reverse.TryGetValue(key, out value);

        /// <summary>
        ///     Add a value to the dictionary with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public void Add(T1 key, T2 value) => Forward.Add(key, value);

        /// <summary>
        ///     Add a value to the dictionary with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public void Add(T2 key, T1 value) => Reverse.Add(key, value);

        /// <summary>
        ///     Attempt to remove an item from the dictionary.
        /// </summary>
        /// <param name="key">
        ///     The key of the item to remove.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the item was removed; otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(T1 key) => Forward.Remove(key);

        /// <summary>
        ///     Attempt to remove an item from the dictionary.
        /// </summary>
        /// <param name="key">
        ///     The key of the item to remove.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the item was removed; otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(T2 key) => Reverse.Remove(key);

        /// <summary>
        ///     Determine whether the dictionary contains an item with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if dictionary contains an item with the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(T1 key) => Forward.ContainsKey(key);

        /// <summary>
        ///     Determine whether the dictionary contains an item with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if dictionary contains an item with the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(T2 key) => Reverse.ContainsKey(key);

        /// <summary>
        ///     An <see cref="IDictionary{TKey, TValue}"/> that encapsulates access to the forward / reverse dictionaries.
        /// </summary>
        class DictionariesWrapper<T3, T4>
            : IDictionary<T3, T4>
        {
            /// <summary>
            ///     Create a new <see cref="DictionariesWrapper{T3, T4}"/>.
            /// </summary>
            /// <param name="forwardDictionary">
            ///     The <see cref="IDictionary{TKey, TValue}"/> used for forward lookups.
            /// </param>
            /// <param name="reverseDictionary">
            ///     The <see cref="IDictionary{TKey, TValue}"/> used for reverse lookups.
            /// </param>
            public DictionariesWrapper(IDictionary<T3, T4> forwardDictionary, IDictionary<T4, T3> reverseDictionary)
            {
                if (forwardDictionary == null)
                    throw new ArgumentNullException(nameof(forwardDictionary));

                if (reverseDictionary == null)
                    throw new ArgumentNullException(nameof(reverseDictionary));

                ForwardLookup = forwardDictionary;
                ReverseLookup = reverseDictionary;
            }

            /// <summary>
            ///     The <see cref="IDictionary{TKey, TValue}"/> used for forward lookups.
            /// </summary>
            IDictionary<T3, T4> ForwardLookup { get; }

            /// <summary>
            ///     The <see cref="IDictionary{TKey, TValue}"/> used for reverse lookups.
            /// </summary>
            IDictionary<T4, T3> ReverseLookup { get; }

            /// <summary>
            ///     Get / set the value with the specified key.
            /// </summary>
            /// <param name="key">
            ///     The value's associated key.
            /// </param>
            public T4 this[T3 key]
            {
                get
                {
                    if (key == null)
                        throw new ArgumentNullException(nameof(key));

                    return ForwardLookup[key];
                }
                set
                {
                    if (key == null)
                        throw new ArgumentNullException(nameof(key));

                    if (value == null)
                        throw new ArgumentNullException(nameof(value));


                    ForwardLookup[key] = value;
                    ReverseLookup[value] = key;
                }
            }

            /// <summary>
            ///     The all the dictionary's keys.
            /// </summary>
            public ICollection<T3> Keys => ForwardLookup.Keys;

            /// <summary>
            ///     The all the dictionary's values.
            /// </summary>
            public ICollection<T4> Values => ForwardLookup.Values;

            /// <summary>
            ///     The number of items in the dictionary.
            /// </summary>
            public int Count => ForwardLookup.Count;

            /// <summary>
            ///     Is the dictionary read-only?
            /// </summary>
            public bool IsReadOnly => false;

            /// <summary>
            ///     Add a value to the dictionary with the specified key.
            /// </summary>
            /// <param name="key">
            ///     The key.
            /// </param>
            /// <param name="value">
            ///     The value.
            /// </param>
            public void Add(T3 key, T4 value)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (ForwardLookup.ContainsKey(key))
                    throw new ArgumentException("The dictionary's forward-lookup section already contains the specified key.", nameof(key));

                if (ReverseLookup.ContainsKey(value))
                    throw new ArgumentException("The dictionary's reverse-lookup section already contains the specified key.", nameof(value));

                ForwardLookup.Add(key, value);
                ReverseLookup.Add(value, key);
            }

            /// <summary>
            ///     Add a key / value pair to the dictionary.
            /// </summary>
            /// <param name="item">
            ///     The <see cref="KeyValuePair{TKey, TValue}"/> to add.
            /// </param>
            public void Add(KeyValuePair<T3, T4> item)
            {
                if (item.Key == null)
                    throw new ArgumentException("Key cannot be null.", nameof(item));

                if (item.Value == null)
                    throw new ArgumentException("Value cannot be null.", nameof(item));

                if (ForwardLookup.ContainsKey(item.Key))
                    throw new ArgumentException("The dictionary's forward-lookup section already contains the specified key.", nameof(item));

                if (ReverseLookup.ContainsKey(item.Value))
                    throw new ArgumentException("The dictionary's reverse-lookup section already contains the specified key.", nameof(item));

                ForwardLookup.Add(item.Key, item.Value);
                ReverseLookup.Add(item.Value, item.Key);
            }

            /// <summary>
            ///     Remove all keys and values from the dictionary.
            /// </summary>
            public void Clear()
            {
                ForwardLookup.Clear();
                ReverseLookup.Clear();
            }

            /// <summary>
            ///     Determine whether the dictionary contains the specified key / value pair.
            /// </summary>
            /// <param name="item">
            ///     The key / value pair to search for.
            /// </param>
            /// <returns>
            ///     <c>true</c>, if the dictionary contains the key and corresponding value; otherwise, <c>false</c>.
            /// </returns>
            public bool Contains(KeyValuePair<T3, T4> item) => ForwardLookup.Contains(item);

            /// <summary>
            ///     Determine whether the dictionary contains the specified key.
            /// </summary>
            /// <param name="key">
            ///     The key.
            /// </param>
            /// <returns>
            ///     <c>true</c>, if the dictionary contains the key; otherwise, <c>false</c>.
            /// </returns>
            public bool ContainsKey(T3 key) => ForwardLookup.ContainsKey(key);

            /// <summary>
            ///     Copy the dictionary contents to the specified array.
            /// </summary>
            /// <param name="array">
            ///     The target array of <see cref="KeyValuePair{TKey, TValue}"/>s.
            /// </param>
            /// <param name="arrayIndex">
            ///     The target index at which to start placing copied items.
            /// </param>
            public void CopyTo(KeyValuePair<T3, T4>[] array, int arrayIndex) => ForwardLookup.CopyTo(array, arrayIndex);

            /// <summary>
            ///     Retrieve a typed enumerator for all key / value pairs in the dictionary.
            /// </summary>
            /// <returns>
            ///     The typed enumerator.
            /// </returns>
            public IEnumerator<KeyValuePair<T3, T4>> GetEnumerator() => ForwardLookup.GetEnumerator();

            /// <summary>
            ///     Attempt to remove a value from the dictionary.
            /// </summary>
            /// <param name="key">
            ///     The key of the value to remove.
            /// </param>
            /// <returns>
            ///     <c>true</c>, if the value was removed; otherwise, <c>false</c>.
            /// </returns>
            public bool Remove(T3 key)
            {
                T4 value;
                if (!ForwardLookup.TryGetValue(key, out value))
                    return false;

                ForwardLookup.Remove(key);
                ReverseLookup.Remove(value);

                return true;
            }

            /// <summary>
            ///     Attempt to remove a key / value pair from the dictionary.
            /// </summary>
            /// <param name="item">
            ///     The key / value pair to remove.
            /// </param>
            /// <returns>
            ///     <c>true</c>, if the key / value pair was removed; otherwise, <c>false</c>.
            /// </returns>
            public bool Remove(KeyValuePair<T3, T4> item)
            {
                if (!ForwardLookup.Remove(item))
                    return false;

                ReverseLookup.Remove(new KeyValuePair<T4, T3>(
                    key: item.Value,
                    value: item.Key
                ));

                return true;
            }

            /// <summary>
            ///     Attempt to retrieve the value with the specified key.
            /// </summary>
            /// <param name="key">
            ///     The key.
            /// </param>
            /// <param name="value">
            ///     Receives the value.
            /// </param>
            /// <returns>
            ///     <c>true</c>, if the value pair retrieved; otherwise, <c>false</c>.
            /// </returns>
            public bool TryGetValue(T3 key, out T4 value) => ForwardLookup.TryGetValue(key, out value);

            /// <summary>
            ///     Retrieve an untyped enumerator for all key / value pairs in the dictionary.
            /// </summary>
            /// <returns>
            ///     The untyped enumerator.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
