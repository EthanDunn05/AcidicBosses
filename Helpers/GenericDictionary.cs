using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AcidicBosses.Helpers;

/// <summary>
/// Acts as a dictionary that can store any type of value.
/// Use this with care as misuse can cause many errors.
/// Always be certain the type matches when getting a value.
/// </summary>
public class GenericDictionary : DictionaryBase
{
    /// <summary>
    /// Gets a value at a given key which is a specific type <see cref="T"/>. 
    /// </summary>
    /// <param name="key">The key of the value</param>
    /// <typeparam name="T">The type of object stored at the key</typeparam>
    /// <returns>The item at the key, cast to type T</returns>
    /// <exception cref="KeyNotFoundException">There is no object with the given key</exception>
    /// <exception cref="InvalidCastException">The value is not of type <see cref="T"/></exception>
    public T Get<T>(string key)
    {
        var retrieved = Dictionary[key];
        if (retrieved == null) throw new KeyNotFoundException();

        // Attempts to cast to T. Lots of possible errors
        return (T) Convert.ChangeType(retrieved, typeof(T));
    }
    
    public void Set<T>(string key, T value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Dictionary[key] = value;
    }

    public void Remove(string key)
    {
        Dictionary.Remove(key);
    }

    public bool Contains(string key)
    {
        return Dictionary.Contains(key);
    }

    /// <summary>
    /// Performs an operation on a value at the given key and saves the result back into the dictionary.
    /// </summary>
    /// <param name="key">The key of the value to operate on</param>
    /// <param name="operation">The function to perform on the value</param>
    /// <typeparam name="T">The type of the value</typeparam>
    public void Operate<T>(string key, Func<T, T> operation)
    {
        var data = Get<T>(key);
        var newData = operation(data);
        Set(key, newData);
    }
}