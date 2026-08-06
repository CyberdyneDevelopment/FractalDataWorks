using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Fdw.Messages;

/// <summary>
/// Base class for message collections that provides core functionality.
/// Classes decorated with [MessageCollection] or [GlobalMessageCollection] attributes
/// can optionally inherit from this base class to get standard collection methods
/// like Get(id), Get(name), and TryGet() without code generation.
/// The source generator will populate the static collection in the static constructor.
/// </summary>
/// <typeparam name="T">The message type that must derive from MessageTemplate</typeparam>
[ExcludeFromCodeCoverage]
public abstract class MessageCollectionBase<T> where T : class, IGenericMessage, ITypeOption<int>
{
    /// <summary>
    /// Static collection of all message options. Populated by the source generator.
    /// </summary>
#pragma warning disable CA2211, MA0069, CA1707, MA0051
    protected static ImmutableArray<T> _all = [];

    /// <summary>
    /// Static empty instance. Populated by the source generator.
    /// </summary>
    protected static T _empty = default!;

    /// <summary>
    /// Internal method to initialize the collection. Called by the source generator.
    /// </summary>
    /// <param name="values">The message values to populate the collection with.</param>
    protected internal static void Initialize(ImmutableArray<T> values)
    {
        _all = values;
        // Set empty to first value that contains "Empty" or default
        _empty = values.FirstOrDefault(v => v.Name.Contains("Empty", StringComparison.OrdinalIgnoreCase)) ?? values.FirstOrDefault() ?? default!;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Lookup dictionary for message options by name (case-insensitive). Populated when first accessed.
    /// </summary>
    private static FrozenDictionary<string, T>? _lookupByName;
    
    /// <summary>
    /// Lookup dictionary for message options by ID. Populated when first accessed.
    /// </summary>
    private static FrozenDictionary<int, T>? _lookupById;
#else
    /// <summary>
    /// Lookup dictionary for message options by name (case-insensitive). Populated when first accessed.
    /// </summary>
    private static ReadOnlyDictionary<string, T>? _lookupByName;

    /// <summary>
    /// Lookup dictionary for message options by ID. Populated when first accessed.
    /// </summary>
    private static ReadOnlyDictionary<int, T>? _lookupById;
#endif
#pragma warning restore CA2211, MA0069, CA1707, MA0051

    /// <summary>
    /// Ensures lookup dictionaries are initialized for fast O(1) lookups.
    /// </summary>
    private static void EnsureLookupsInitialized()
    {
        if (_lookupByName != null) return;

        // Handle duplicates with "last value wins" semantics
        var nameDict = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        var idDict = new Dictionary<int, T>();

        foreach (var item in _all)
        {
            nameDict[item.Name] = item; // Overwrites duplicates
            idDict[item.Id] = item;     // Overwrites duplicates
        }

#if NET8_0_OR_GREATER
        _lookupByName = nameDict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _lookupById = idDict.ToFrozenDictionary();
#else
        _lookupByName = new ReadOnlyDictionary<string, T>(nameDict);
        _lookupById = new ReadOnlyDictionary<int, T>(idDict);
#endif
    }

    /// <summary>
    /// Gets all message options in this collection.
    /// </summary>
    public static ImmutableArray<T> All() => _all;

    /// <summary>
    /// Gets an empty instance of the message option type.
    /// </summary>
    public static T Empty() => _empty;

    /// <summary>
    /// Gets a message option by name (case-insensitive).
    /// </summary>
    public static T? Get(string? name)
    {
        if (name is null || string.IsNullOrWhiteSpace(name)) return null;

        EnsureLookupsInitialized();
        return _lookupByName!.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a message option by ID.
    /// </summary>
    public static T? Get(int id)
    {
        EnsureLookupsInitialized();
        return _lookupById!.TryGetValue(id, out var value) ? value : null;
    }

    /// <summary>
    /// Tries to get a message option by name
    /// </summary>
    public static bool TryGet(string? name, out T? value)
    {
        value = Get(name);
        return value != null;
    }

    /// <summary>
    /// Tries to get a message option by ID
    /// </summary>
    public static bool TryGet(int id, out T? value)
    {
        value = Get(id);
        return value != null;
    }

    /// <summary>
    /// Gets all message options as an enumerable
    /// </summary>
    public static IEnumerable<T> AsEnumerable() => _all;

    /// <summary>
    /// Gets the count of message options
    /// </summary>
    public static int Count => _all.Length;

    /// <summary>
    /// Checks if the collection contains any items
    /// </summary>
    public static bool Any() => _all.Length > 0;
}