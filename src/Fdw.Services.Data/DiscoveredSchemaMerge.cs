using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;

namespace Fdw.Services.Data;

/// <summary>
/// Adds what a schema discovery found to a data store's configuration: paths, containers, fields,
/// and keys. The one merge every discovery path uses (<see cref="SchemaInformationService"/>).
/// </summary>
/// <remarks>
/// <para>
/// Nothing is ever removed. A path or container the store does not have is added. One it has is kept
/// as it is on a plain discovery, because its fields and keys may be referenced by data sets. A refresh
/// re-reads the source: the container's type, and each existing field's data type and nullability, are
/// brought in line with it, and fields the source has and the container lacks are added.
/// </para>
/// <para>
/// A field records two types (FDW-1588): <c>DataType</c> is the store's own (<c>nvarchar</c>), as the discovered field's
/// <c>FieldType.TypeName</c> reports it, and <c>FrameworkDataType</c> is the portable type the store reads it as
/// (<c>String</c>), the discovered field's <c>ExplicitType</c>. A store type FDW has no native type for leaves the portable
/// type unstated for a person to state.
/// </para>
/// <para>
/// A discovered field whose datum type identifies its record (a primary-key column is discovered as
/// Identifier) becomes a key field of the container's Surrogate key, named <c>Surrogate_{Container}</c>
/// (FDW-988).
/// </para>
/// <para>
/// The store is changed in place and not saved: the caller saves it, once, with whatever else the
/// same request changed.
/// </para>
/// </remarks>
internal static class DiscoveredSchemaMerge
{
    /// <summary>Adds the discovered containers the store lacks.</summary>
    /// <param name="store">The store; changed in place.</param>
    /// <param name="discovered">What the connection's schema discovery returned.</param>
    /// <param name="refresh">Whether containers the store already has are brought in line with the source.</param>
    /// <returns>What was found, and what was added.</returns>
    internal static DataStoreSchemaImportResult Merge(
        IDataStoreImplementationConfiguration store, IReadOnlyList<IStorageContainer> discovered, bool refresh)
    {
        var pathsAdded = 0;
        var containersAdded = 0;
        var fieldsAdded = 0;
        var fieldsUpdated = 0;

        foreach (var source in discovered)
        {
            var path = store.Paths.FirstOrDefault(
                p => string.Equals(p.PathValue, source.Path.PathValue, StringComparison.OrdinalIgnoreCase));
            if (path is null)
            {
                path = new DataPathConfiguration { Name = source.Path.PathValue, PathValue = source.Path.PathValue };
                store.Paths.Add(path);
                pathsAdded++;
            }

            var existing = path.Containers.FirstOrDefault(
                c => string.Equals(c.Name, source.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var container = ContainerFrom(source);
                path.Containers.Add(container);
                containersAdded++;
                fieldsAdded += container.Fields.Count;
                continue;
            }

            if (!refresh)
                continue;

            existing.TypeId = source.ContainerType.Name;
            fieldsUpdated += UpdateExistingFields(existing, source);
            fieldsAdded += AddMissingFields(existing, source);
        }

        return new DataStoreSchemaImportResult(
            discovered.Count, discovered.Sum(c => c.Schema.Fields.Count), pathsAdded, containersAdded, fieldsAdded, fieldsUpdated);
    }

    private static DataContainerConfiguration ContainerFrom(IStorageContainer source)
    {
        var fields = source.Schema.Fields.Select((field, index) => FieldFrom(field, index + 1)).ToList();
        return new DataContainerConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = source.Name,
            TypeId = source.ContainerType.Name,
            Format = source.Format.Name,
            Fields = fields,
            Keys = KeysFrom(source, fields),
        };
    }

    private static DataContainerFieldConfiguration FieldFrom(IField field, int ordinal) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = field.Name,
        DataType = field.FieldType.TypeName,
        FrameworkDataType = field.ExplicitType?.Name,
        IsNullable = field.IsNullable,
        Ordinal = ordinal,
        IsSystemProvided = field.IsSystemProvided || field.IsIdentity || field.IsComputed,
        HasDefault = field.HasDefault,
        DatumTypeId = field.Role.Id,
        Description = field.Description,
    };

    // Why the datum type's IsKeyRole and not its name: a datum type says what a column's data is, a key
    // type says what a key is, and the two collections share no names. Before FDW-988 discovery gave a
    // primary-key column the Surrogate role, a name KeyTypes also has, and this merge read the key type off
    // it. Discovery now types the column Identifier; the key it belongs to is still the container's
    // Surrogate key, exactly as before, so every reader that looks a discovered key up as Surrogate is
    // unchanged.
    private static List<DataContainerKeyConfiguration> KeysFrom(IStorageContainer source, List<DataContainerFieldConfiguration> fields)
    {
        var keyFields = source.Schema.Fields
            .Select((field, index) => (field.Role, Field: fields[index]))
            .Where(entry => entry.Role.IsKeyRole)
            .ToList();
        if (keyFields.Count == 0)
            return [];

        var keyType = KeyTypes.ByName("Surrogate").Name;
        return
        [
            new DataContainerKeyConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = keyType + "_" + source.Name,
                TypeId = keyType,
                KeyFields = keyFields.Select((entry, position) => new DataContainerKeyFieldConfiguration
                {
                    Id = Guid.CreateVersion7(),
                    DataContainerFieldId = entry.Field.Id,
                    Ordinal = position + 1,
                    KeyType = keyType,
                }).ToList(),
            },
        ];
    }

    // A refresh re-reads the source, so a column whose type or nullability changed there changes here, and a field discovered
    // before FDW-1588 (a .NET name in DataType, no portable type) is repaired. A portable type already stated, by a person or
    // by discovery, is never replaced. Everything else about the field (its id, ordinal, visibility, description) is left as it is.
    private static int UpdateExistingFields(DataContainerConfiguration existing, IStorageContainer source)
    {
        var updated = 0;
        foreach (var field in source.Schema.Fields)
        {
            var match = existing.Fields.FirstOrDefault(f => string.Equals(f.Name, field.Name, StringComparison.OrdinalIgnoreCase));
            if (match is null)
                continue;

            var portableUnstated = string.IsNullOrEmpty(match.FrameworkDataType) && field.ExplicitType is not null;
            if (string.Equals(match.DataType, field.FieldType.TypeName, StringComparison.Ordinal)
                && match.IsNullable == field.IsNullable
                && !portableUnstated)
                continue;

            match.DataType = field.FieldType.TypeName;
            match.IsNullable = field.IsNullable;
            if (string.IsNullOrEmpty(match.FrameworkDataType) && field.ExplicitType is { } portable)
                match.FrameworkDataType = portable.Name;
            updated++;
        }

        return updated;
    }

    private static int AddMissingFields(DataContainerConfiguration existing, IStorageContainer source)
    {
        var known = existing.Fields.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var next = existing.Fields.Count == 0 ? 1 : existing.Fields.Max(f => f.Ordinal) + 1;
        var added = 0;
        foreach (var field in source.Schema.Fields.Where(f => !known.Contains(f.Name)))
        {
            existing.Fields.Add(FieldFrom(field, next++));
            added++;
        }

        return added;
    }
}
