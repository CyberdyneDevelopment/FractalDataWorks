using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Extension methods for <see cref="IStorageContainer"/>.
/// </summary>
/// <remarks>
/// Every answer here is read from <see cref="IStorageContainer.Keys"/>, the one place a container states its keys. An earlier
/// overload on <see cref="IStorageContainer"/> read the entry <c>SurrogateKeyField</c> from the container's metadata, which no
/// builder ever wrote, so it answered nothing for a built container (FDW-6309).
/// </remarks>
public static class StorageContainerExtensions
{
    /// <summary>
    /// Returns the columns of the container's primary key, the whole key, in key order.
    /// </summary>
    /// <param name="container">The storage container.</param>
    /// <returns>
    /// The local field names of the first key whose type is a primary key and that names at least one field, ordered by the
    /// key's own ordinals; an empty list when the container declares no such key.
    /// </returns>
    /// <remarks>
    /// A caller that writes by the key (an update that matches on it, a page that follows it) uses every column returned.
    /// Reading only the first column of a composite key matches, and so writes, every row that shares that value.
    /// </remarks>
    public static IReadOnlyList<string> PrimaryKeyFieldNames(this IStorageContainer container)
    {
        if (container is null)
            return [];

        var keys = container.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (key.KeyType.IsPrimaryKey && key.KeyFields.Count > 0)
                return [.. key.KeyFields.OrderBy(keyField => keyField.Ordinal).Select(keyField => keyField.LocalField.Name)];
        }

        return [];
    }

    /// <summary>
    /// Returns the primary key's column name when the key is exactly one column.
    /// </summary>
    /// <param name="container">The storage container.</param>
    /// <returns>
    /// The column name of a one-column primary key; <c>null</c> when the container declares no primary key and also when its
    /// key has more than one column. Never the first column of a composite key: use <see cref="PrimaryKeyFieldNames"/>.
    /// </returns>
    public static string? GetPrimaryKeyFieldName(this IStorageContainer container)
    {
        var names = container.PrimaryKeyFieldNames();
        return names.Count == 1 ? names[0] : null;
    }

    /// <summary>
    /// Returns the columns of the container that are part of a foreign key it declares, whether or not the container the key references is known.
    /// </summary>
    /// <param name="container">The storage container.</param>
    /// <returns>
    /// The local field names of every key whose type references another container, in the order the keys are declared, each once; an
    /// empty list when the container declares none. A column of a foreign key whose target a discovery did not read is still a foreign key
    /// column, so it is answered here.
    /// </returns>
    public static IReadOnlyList<string> ForeignKeyFieldNames(this IStorageContainer container)
    {
        if (container is null)
            return [];

        return [.. container.Keys
            .Where(key => key.KeyType.IsReference)
            .SelectMany(key => key.KeyFields)
            .Select(keyField => keyField.LocalField.Name)
            .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Returns the column pairs of the foreign keys the container declares whose target is known.
    /// </summary>
    /// <param name="container">The storage container.</param>
    /// <returns>
    /// One pair for each key field of each foreign key that names the container and the column it references. A key whose target is
    /// unresolved has no pair, so a caller that draws relationships draws none for it, while <see cref="ForeignKeyFieldNames"/> still
    /// names its columns.
    /// </returns>
    public static IReadOnlyList<ForeignKeyColumnPair> ForeignKeyColumnPairs(this IStorageContainer container)
    {
        if (container is null)
            return [];

        return [.. container.Keys
            .Where(key => key.KeyType.IsReference && key.ReferencedContainer is not null)
            .SelectMany(key => key.KeyFields
                .Where(keyField => keyField.ReferencedField is not null)
                .OrderBy(keyField => keyField.Ordinal)
                .Select(keyField => new ForeignKeyColumnPair(key.KeyName, keyField.LocalField.Name, key.ReferencedContainer!, keyField.ReferencedField!.Name)))];
    }

    /// <summary>
    /// Returns the columns a page of this container's rows follows when the query states no ordering: the container's primary
    /// key, the whole key, in key order (FDW-3454, FDW-6309).
    /// </summary>
    /// <param name="container">The storage container.</param>
    /// <returns>
    /// The declared key's column names; an empty list when the container declares none, and a caller refuses the page then,
    /// because no order is assumed. A composite key is ordered by every column: ordering by the first only leaves rows that
    /// share it in no fixed order, and a row can repeat or be skipped between two pages.
    /// </returns>
    public static IReadOnlyList<string> DeclaredOrderFieldNames(this IStorageContainer container)
        => container.PrimaryKeyFieldNames();
}
