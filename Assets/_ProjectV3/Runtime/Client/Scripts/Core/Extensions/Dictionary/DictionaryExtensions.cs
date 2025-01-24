using System.Collections.Generic;

namespace _Project.Runtime.Core.Extensions.Dictionary
{
    public static class DictionaryExtensions
    {
        public static void Update<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }
        public static void Update<TKey, TCollection, TValue>(this IDictionary<TKey, TCollection> dictionary, TKey key, TValue value)
            where TCollection : ICollection<TValue>, new()
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, new TCollection());
            }

            dictionary[key].Add(value);
        }
    }
}
