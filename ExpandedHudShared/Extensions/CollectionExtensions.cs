namespace ExpandedHudShared.Extensions;

/// <summary>
///   Record that provides a value, and whether it was created in a collection
/// </summary>
/// <param name="Result">Some return value</param>
/// <param name="WasCreated">If this return value had to be created, or if it already existed</param>
/// <typeparam name="T">Any Type T</typeparam>
public record GetOrCreateResult<T>(T Result, bool WasCreated);

/// <summary> Extensions for collections </summary>
public static class CollectionExtensions
{
  /// <summary>
  ///   Get a value from a dictionary, or return a default if it isn't present in the collection
  /// </summary>
  /// <param name="dictionary">This Dictionary</param>
  /// <param name="key">The key to look up</param>
  /// <param name="defaultValue">The value to return if the key is not present</param>
  /// <typeparam name="TKey">Dictionary Key Type</typeparam>
  /// <typeparam name="TValue">Dictionary Value Type</typeparam>
  /// <returns>dictionary[key] or defaultValue</returns>
  public static TValue GetOrDefault<TKey, TValue>(
    this IDictionary<TKey, TValue> dictionary,
    TKey key,
    TValue defaultValue
  )
  {
    return dictionary.TryGetValue(key, out TValue? foundDictValue) ? foundDictValue : defaultValue;
  }

  /// <summary>
  /// Get a value from a dictionary, or return the language default if it isn't present in the collection
  /// </summary>
  /// <param name="dictionary">This Dictionary</param>
  /// <param name="key">The key to look up</param>
  /// <typeparam name="TKey">Dictionary Key Type</typeparam>
  /// <typeparam name="TValue">Dictionary Value Type</typeparam>
  /// <returns>dictionary[key], or default(TValue)</returns>
  public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    where TValue : unmanaged
  {
    return dictionary.GetOrDefault(key, default);
  }

  /// <summary>
  ///   Get a value from a dictionary, or create and insert it if it doesn't exist using default constructible.
  /// </summary>
  /// <param name="dictionary">This Dictionary</param>
  /// <param name="key">The key to look up</param>
  /// <typeparam name="TKey">Dictionary Key Type</typeparam>
  /// <typeparam name="TValue">Dictionary Value Type, must be default constructible</typeparam>
  /// <returns><see cref="GetOrCreateResult{T}" /> - A result with the value, and an indicator of whether it was created</returns>
  public static GetOrCreateResult<TValue> GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    where TValue : new()
  {
    if (dictionary.TryGetValue(key, out TValue? value))
    {
      return new GetOrCreateResult<TValue>(value, false);
    }

    dictionary[key] = new TValue();

    return new GetOrCreateResult<TValue>(dictionary[key], true);
  }

  /// <summary>
  ///   Get a value from a dictionary, or create and insert it if it doesn't exist using a provided function.
  /// </summary>
  /// <param name="dictionary">This Dictionary</param>
  /// <param name="key">The key to look up</param>
  /// <param name="defaultCreate">Function to create the default value</param>
  /// <typeparam name="TKey">Dictionary Key Type</typeparam>
  /// <typeparam name="TValue">Dictionary Value Type</typeparam>
  /// <returns><see cref="GetOrCreateResult{T}" /> - A result with the value, and an indicator of whether it was created</returns>
  public static GetOrCreateResult<TValue> GetOrCreate<TKey, TValue>(
    this IDictionary<TKey, TValue> dictionary,
    TKey key,
    Func<TValue> defaultCreate
  )
  {
    if (dictionary.TryGetValue(key, out TValue? value))
    {
      return new GetOrCreateResult<TValue>(value, false);
    }

    dictionary[key] = defaultCreate();

    return new GetOrCreateResult<TValue>(dictionary[key], true);
  }

  /// <summary>
  ///   Shuffles an Enumerable, returning a new randomized sequence
  /// </summary>
  /// <param name="source">The input enumerable</param>
  /// <typeparam name="T">Some Type T</typeparam>
  /// <returns>A new shuffled enumerable, from values from the source.</returns>
  public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
  {
    var random = new Random();
    List<T> shuffledList = source.ToList();
    int n = shuffledList.Count;
    while (n > 1)
    {
      n--;
      int k = random.Next(n + 1);
      (shuffledList[k], shuffledList[n]) = (shuffledList[n], shuffledList[k]);
    }

    return shuffledList;
  }
}
