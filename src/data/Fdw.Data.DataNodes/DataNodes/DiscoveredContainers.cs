using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Builds the containers a schema discovery returns, each carrying the primary key and the foreign keys the catalog declares on it in
/// the existing <see cref="IContainerKey"/> model (FDW-6309).
/// </summary>
/// <remarks>
/// <para>
/// Two waves, as <c>DataStoreBuilderBase</c> does for a configured store: every container is first built bare, with no keys, so a
/// foreign key can name the container it references whatever order the discovery read them in and whether or not two containers
/// reference each other; then each container is built again with its keys, a foreign key's <see cref="IContainerKey.ReferencedContainer"/>
/// being the bare container.
/// </para>
/// <para>
/// A foreign key whose target is not among the containers of the discovery (its schema was left out, or it is a view) stays on its
/// container as a Foreign key with no referenced container, so the column is still known to be a foreign key, and one coded warning
/// says which key and which target, so nothing is dropped without being said. A key naming a column its container lacks refuses the
/// whole discovery.
/// </para>
/// </remarks>
public static class DiscoveredContainers
{
    private const string PrimaryKeyTypeName = "PrimaryKey";
    private const string ForeignKeyTypeName = "Foreign";

    /// <summary>
    /// Builds the containers of a discovery with their keys.
    /// </summary>
    /// <param name="specs">What the discovery read, in the order it read it.</param>
    /// <param name="messages">The coded messages the discovering connection kind gives for a foreign key it cannot resolve and a key column that is missing.</param>
    /// <returns>
    /// The containers in the order of <paramref name="specs"/>, a success with one warning for each foreign key whose target the
    /// discovery did not read, or the refusal when a key names a column its container does not have.
    /// </returns>
    public static IGenericResult<IReadOnlyList<IDataContainer>> Assemble(
        IReadOnlyList<DiscoveredContainerSpec> specs,
        DiscoveredKeyMessages messages)
    {
        var primaryKeyType = KeyTypes.ByName(PrimaryKeyTypeName);
        var foreignKeyType = KeyTypes.ByName(ForeignKeyTypeName);
        if (ReferenceEquals(primaryKeyType, KeyTypes.NotFound) || ReferenceEquals(foreignKeyType, KeyTypes.NotFound))
            return GenericResult<IReadOnlyList<IDataContainer>>.Failure(DiscoveredContainersLog.KeyTypeNotRegistered(
                NullLogger.Instance,
                ReferenceEquals(primaryKeyType, KeyTypes.NotFound) ? PrimaryKeyTypeName : ForeignKeyTypeName));

        var fieldsBySpec = specs.Select(spec => (IReadOnlyList<IDataField>)[.. spec.Fields.Select((field, index) => new DiscoveredDataField(field, index + 1))]).ToList();
        var bare = new Dictionary<(string Schema, string Name), IDataContainer>();
        for (var i = 0; i < specs.Count; i++)
            bare[(specs[i].SchemaName, specs[i].Name)] = Build(specs[i], fieldsBySpec[i], [], NoReferencingKeys());

        var warnings = new List<IGenericMessage>();
        var keysBySpec = new List<IReadOnlyList<IContainerKey>>(specs.Count);
        for (var i = 0; i < specs.Count; i++)
        {
            var keys = KeysOf(specs[i], fieldsBySpec[i], bare, (KeyTypeBase)primaryKeyType, (KeyTypeBase)foreignKeyType, messages, warnings, out var refused);
            if (refused is not null)
                return GenericResult<IReadOnlyList<IDataContainer>>.Failure(refused);

            keysBySpec.Add(keys);
        }

        var referencing = ReferencingKeysByContainer(specs, keysBySpec, bare);
        var containers = new List<IDataContainer>(specs.Count);
        for (var i = 0; i < specs.Count; i++)
            containers.Add(Build(specs[i], fieldsBySpec[i], keysBySpec[i], referencing[(specs[i].SchemaName, specs[i].Name)]));

        return warnings.Count == 0
            ? GenericResult<IReadOnlyList<IDataContainer>>.Success(containers)
            : GenericResult<IReadOnlyList<IDataContainer>>.Success(containers, ResultStatuses.ByName("SuccessWithWarnings"), warnings.ToArray());
    }

    /// <summary>
    /// Gives the result of <see cref="Assemble"/> as the storage containers a schema discovery returns, with everything the assembly said
    /// kept: a refusal stays a refusal, and a success keeps its warnings and its status.
    /// </summary>
    /// <param name="assembled">The result of <see cref="Assemble"/>.</param>
    /// <returns>The same containers as <see cref="IStorageContainer"/>, with the messages and status of <paramref name="assembled"/>.</returns>
    public static IGenericResult<IReadOnlyList<IStorageContainer>> AsStorageContainers(IGenericResult<IReadOnlyList<IDataContainer>> assembled)
    {
        if (assembled.IsFailure || assembled.Value is not { } containers)
            return assembled.ToNewResult<IReadOnlyList<IStorageContainer>>();

        IReadOnlyList<IStorageContainer> storage = containers;
        return assembled.Messages.Count == 0
            ? GenericResult<IReadOnlyList<IStorageContainer>>.Success(storage)
            : GenericResult<IReadOnlyList<IStorageContainer>>.Success(storage, assembled.Status, assembled.Messages.ToArray());
    }

    /// <summary>
    /// Adds warnings the discovering connection kind gave while reading the catalog to the result of assembling its containers.
    /// </summary>
    /// <param name="assembled">The result of <see cref="AsStorageContainers"/>.</param>
    /// <param name="warnings">The warnings the kind's discovery returned with its success; none leaves the result as it is.</param>
    /// <returns>A refusal as it is; otherwise a success with the warnings of the assembly followed by <paramref name="warnings"/>.</returns>
    public static IGenericResult<IReadOnlyList<IStorageContainer>> WithWarnings(
        IGenericResult<IReadOnlyList<IStorageContainer>> assembled,
        IReadOnlyList<IGenericMessage> warnings)
    {
        if (assembled.IsFailure || assembled.Value is not { } containers || warnings.Count == 0)
            return assembled;

        return GenericResult<IReadOnlyList<IStorageContainer>>.Success(
            containers,
            ResultStatuses.ByName("SuccessWithWarnings"),
            [.. assembled.Messages, .. warnings]);
    }

    private static IGenericResult<IReadOnlyList<ReferencingKeyBinding>> NoReferencingKeys()
        => GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]);

    private static DataContainer Build(
        DiscoveredContainerSpec spec,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys)
        => new(
            spec.Name,
            spec.SchemaName,
            fields,
            keys,
            referencingKeys,
            spec.ContainerType,
            spec.Format,
            spec.Path,
            [.. spec.ContainerType.SupportedOperations],
            spec.Metadata);

    private static List<IContainerKey> KeysOf(
        DiscoveredContainerSpec spec,
        IReadOnlyList<IDataField> fields,
        Dictionary<(string Schema, string Name), IDataContainer> bare,
        KeyTypeBase primaryKeyType,
        KeyTypeBase foreignKeyType,
        DiscoveredKeyMessages messages,
        List<IGenericMessage> warnings,
        out IGenericMessage? refused)
    {
        refused = null;
        var keys = new List<IContainerKey>();
        var local = fields.ToDictionary(field => field.Name, StringComparer.Ordinal);

        if (spec.PrimaryKeyColumns.Count > 0)
        {
            var keyName = "PK_" + spec.Name;
            var keyFields = new List<IContainerKeyField>(spec.PrimaryKeyColumns.Count);
            for (var position = 0; position < spec.PrimaryKeyColumns.Count; position++)
            {
                if (!local.TryGetValue(spec.PrimaryKeyColumns[position], out var column))
                {
                    refused = messages.ColumnNotFound(keyName, spec.SchemaName, spec.Name, spec.PrimaryKeyColumns[position]);
                    return keys;
                }

                keyFields.Add(new ContainerKeyField(column, null, position));
            }

            keys.Add(new ContainerKey(keyName, null, primaryKeyType, isPhysical: true, isOwner: false, referencedContainer: null, keyFields));
        }

        foreach (var foreignKey in spec.ForeignKeys)
        {
            bare.TryGetValue((foreignKey.ReferencedSchema, foreignKey.ReferencedTable), out var target);
            var targetFields = target?.Nodes.OfType<IDataField>().ToDictionary(field => field.Name, StringComparer.Ordinal);
            var keyFields = new List<IContainerKeyField>(foreignKey.Columns.Count);
            for (var position = 0; position < foreignKey.Columns.Count; position++)
            {
                var pair = foreignKey.Columns[position];
                if (!local.TryGetValue(pair.Column, out var column))
                {
                    refused = messages.ColumnNotFound(foreignKey.Name, spec.SchemaName, spec.Name, pair.Column);
                    return keys;
                }

                IDataField? referencedColumn = null;
                if (targetFields is not null && !targetFields.TryGetValue(pair.ReferencedColumn, out referencedColumn))
                {
                    refused = messages.ColumnNotFound(foreignKey.Name, foreignKey.ReferencedSchema, foreignKey.ReferencedTable, pair.ReferencedColumn);
                    return keys;
                }

                keyFields.Add(new ContainerKeyField(column, referencedColumn, position));
            }

            if (target is null)
                warnings.Add(messages.TargetNotDiscovered(foreignKey.Name, spec.SchemaName, spec.Name, foreignKey.ReferencedSchema, foreignKey.ReferencedTable));

            keys.Add(new ContainerKey(foreignKey.Name, null, foreignKeyType, isPhysical: true, isOwner: false, target, keyFields));
        }

        return keys;
    }

    // The inbound foreign keys of each container: every key that names it as its target, with the container that holds the key.
    private static Dictionary<(string Schema, string Name), IGenericResult<IReadOnlyList<ReferencingKeyBinding>>> ReferencingKeysByContainer(
        IReadOnlyList<DiscoveredContainerSpec> specs,
        List<IReadOnlyList<IContainerKey>> keysBySpec,
        Dictionary<(string Schema, string Name), IDataContainer> bare)
    {
        var inbound = specs.ToDictionary(spec => (spec.SchemaName, spec.Name), _ => new List<ReferencingKeyBinding>());
        // A container class does not override equality, so the default comparer finds a bare container by reference.
        var addressOf = bare.ToDictionary(entry => entry.Value, entry => entry.Key);
        for (var i = 0; i < specs.Count; i++)
        {
            foreach (var key in keysBySpec[i].Where(key => key.ReferencedContainer is not null))
                inbound[addressOf[key.ReferencedContainer!]].Add(new ReferencingKeyBinding(key, bare[(specs[i].SchemaName, specs[i].Name)]));
        }

        return inbound.ToDictionary(
            entry => entry.Key,
            entry => (IGenericResult<IReadOnlyList<ReferencingKeyBinding>>)GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(entry.Value));
    }
}
