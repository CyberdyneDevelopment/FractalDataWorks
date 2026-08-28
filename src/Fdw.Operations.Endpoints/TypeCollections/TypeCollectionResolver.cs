using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Collections;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Resolves TypeCollection types by name and retrieves their values using reflection.
/// </summary>
/// <remarks>
/// Why: TypeCollections (ConnectionTypes, MsSqlAuthenticationTypes, FilterOperators, etc.)
/// are concrete classes that live in many assemblies loaded by the entry-point host, not by
/// the endpoints assembly itself. The endpoints project does not (and must not) reference the
/// concrete connection/auth packages, so Type.GetType(name) cannot find them. The only viable
/// resolution is a bounded scan of the loaded assemblies for a public type whose simple name
/// matches and that exposes a static parameterless All() returning ITypeOption values. Results
/// are cached so the scan runs at most once per distinct collection name.
/// </remarks>
internal static class TypeCollectionResolver
{
    private static readonly ConcurrentDictionary<string, Type?> ResolvedCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Finds the CLR type for a TypeCollection by its simple name (case-insensitive).
    /// </summary>
    /// <param name="collectionName">The simple name of the TypeCollection (e.g., "ConnectionTypes").</param>
    /// <returns>The CLR type, or null if not found.</returns>
    /// <remarks>
    /// Why: scans the loaded assemblies for a public, non-generic class whose simple name matches
    /// the request and that carries a static parameterless All() method — the canonical
    /// TypeCollection accessor. Module initializers from Registration.SourceGenerators ensure the
    /// concrete option assemblies are loaded before any request reaches this endpoint.
    /// </remarks>
    public static Type? FindCollectionType(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            return null;

        return ResolvedCache.GetOrAdd(collectionName, ScanForCollectionType);
    }

    private static Type? ScanForCollectionType(string collectionName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (!type.IsClass || type.IsGenericTypeDefinition)
                    continue;

                if (!string.Equals(type.Name, collectionName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (type.GetMethod("All", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null) is not null)
                    return type;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all TypeOption values from a TypeCollection type using reflection.
    /// </summary>
    /// <param name="collectionType">The TypeCollection CLR type.</param>
    /// <returns>All TypeOption instances, or empty if the collection's All() method cannot be found.</returns>
    public static IReadOnlyList<ITypeOption> GetValues(Type collectionType)
    {
        var allMethod = collectionType.GetMethod("All", BindingFlags.Public | BindingFlags.Static);
        if (allMethod == null)
            return [];

        var result = allMethod.Invoke(null, null);
        if (result == null)
            return [];

        var values = new List<ITypeOption>();
        foreach (var item in (System.Collections.IEnumerable)result)
        {
            var candidate = UnwrapKeyValuePair(item);

            if (candidate is ITypeOption typeOption)
                values.Add(typeOption);
        }

        return values;
    }

    /// <summary>
    /// Returns the dictionary entry's value when <paramref name="item"/> is a
    /// <see cref="KeyValuePair{TKey, TValue}"/> (as yielded by a frozen-dictionary All()),
    /// otherwise returns the item unchanged.
    /// </summary>
    private static object? UnwrapKeyValuePair(object? item)
    {
        if (item == null)
            return null;

        var itemType = item.GetType();
        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            return itemType.GetProperty("Value")?.GetValue(item);

        return item;
    }

    /// <summary>
    /// Maps a TypeOption to a summary DTO, extracting ExpectedProperties and RequiredProperties
    /// via reflection when available.
    /// </summary>
    public static TypeCollectionValueSummaryDto ToSummary(ITypeOption typeOption)
    {
        return new TypeCollectionValueSummaryDto
        {
            Name = typeOption.Name,
            ExpectedProperties = GetStringListProperty(typeOption, "ExpectedProperties"),
            RequiredProperties = GetStringListProperty(typeOption, "RequiredProperties")
        };
    }

    /// <summary>
    /// Extracts a string list property from an object via reflection.
    /// Returns an empty list if the property does not exist or is not a string collection.
    /// </summary>
    private static IReadOnlyList<string> GetStringListProperty(object obj, string propertyName)
    {
        var propInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propInfo == null)
            return [];

        var value = propInfo.GetValue(obj);
        if (value is IReadOnlyList<string> readOnlyList)
            return readOnlyList;

        if (value is IEnumerable<string> enumerable)
            return enumerable.ToList();

        return [];
    }
}
