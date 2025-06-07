namespace Volatility.Utilities;

static class DictUtilities
{
    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
    {
        if (!dict.TryGetValue(key, out var val))
        {
            val = factory();
            dict[key] = val;
        }
        return val;
    }
}
