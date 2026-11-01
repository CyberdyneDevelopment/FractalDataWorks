using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// The writes that make up one configuration record, as a single unit: a row and every child that
/// hangs from it commit together or not at all.
/// </summary>
/// <remarks>
/// Why this exists rather than the providers calling the gateway directly: a record is a row plus its
/// children, written by several statements and, for a domain record, by two providers. Written one
/// statement at a time each commits on its own, so a failure part way through leaves a record that is
/// half in the database and can never be composed back. It also records every container it writes to,
/// because a write inside a transaction does not invalidate the gateway's cache the way a direct write
/// does — the cached reads have to be dropped after the commit, and only for the containers the commit
/// actually changed.
/// </remarks>
public sealed class ConfigurationWriteScope : IAsyncDisposable
{
    private readonly IDataGatewayTransaction _transaction;
    private readonly IConfigurationGateway _gateway;
    private readonly string _dataStoreName;
    private readonly ILogger _logger;
    private readonly List<DataStoreTarget> _written = [];

    private ConfigurationWriteScope(IDataGatewayTransaction transaction, IConfigurationGateway gateway, string dataStoreName, ILogger logger)
    {
        _transaction = transaction;
        _gateway = gateway;
        _dataStoreName = dataStoreName;
        _logger = logger;
    }

    /// <summary>Opens a unit of work over the store a provider is bound to.</summary>
    /// <param name="gateway">The gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the record's rows live on.</param>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The open scope, or the failure that prevented it.</returns>
    internal static async Task<IGenericResult<ConfigurationWriteScope>> Begin(
        IConfigurationGateway gateway,
        string dataStoreName,
        ILogger logger,
        CancellationToken ct)
    {
        var transaction = await gateway.BeginTransaction(dataStoreName, ct).ConfigureAwait(false);
        return transaction.IsSuccess && transaction.Value is { } opened
            ? GenericResult<ConfigurationWriteScope>.Success(new ConfigurationWriteScope(opened, gateway, dataStoreName, logger))
            : transaction.ToNewResult<ConfigurationWriteScope>();
    }

    /// <summary>Runs one write of this record inside the unit of work.</summary>
    internal async Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken ct)
    {
        _written.Add(target);
        return await _transaction.Execute<T>(command, target, ct).ConfigureAwait(false);
    }

    /// <summary>Runs one configuration write -- a save or a retire -- and returns the key values it reports.</summary>
    internal async Task<IGenericResult<ConfigurationWriteResult>> Write(IDataCommand command, DataStoreTarget target, CancellationToken ct)
    {
        _written.Add(target);
        return await _transaction.Execute<ConfigurationWriteResult>(command, target, ct).ConfigureAwait(false);
    }

    /// <summary>The loaded container <paramref name="containerName"/> on <paramref name="pathName"/> of this unit's store.</summary>
    internal IGenericResult<IDataContainer> Container(string pathName, string containerName)
    {
        var store = _gateway.DataStores.FirstOrDefault(s => string.Equals(s.Name, _dataStoreName, StringComparison.Ordinal));
        if (store is null)
            return GenericResult<IDataContainer>.Failure(
                DefaultConfigurationProviderLog.DataStoreNotFound(_logger, containerName, _dataStoreName));

        var path = store.Path(pathName);
        if (path.IsFailure || path.Value is null)
            return GenericResult<IDataContainer>.Failure(
                DefaultConfigurationProviderLog.PathNotFound(_logger, containerName, pathName, _dataStoreName));

        var container = path.Value.Container(containerName);
        return container.IsSuccess && container.Value is not null
            ? GenericResult<IDataContainer>.Success(container.Value)
            : GenericResult<IDataContainer>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, containerName, containerName, _dataStoreName));
    }

    /// <summary>
    /// The values of the Foreign key <paramref name="childContainer"/> declares to <paramref name="parent"/>,
    /// taken from the parent row's key values: each key field gets the value of the field it references.
    /// </summary>
    /// <remarks>The key is the child's owner key to the parent; see <see cref="OwnerBinding"/>.</remarks>
    internal IGenericResult<IReadOnlyDictionary<string, object?>> ForeignKeyValues(
        IDataContainer parent, string childContainer, IReadOnlyDictionary<string, object?> parentKeyValues)
    {
        var binding = OwnerBinding(_logger, parent, childContainer);
        if (binding.IsFailure) return binding.ToNewResult<IReadOnlyDictionary<string, object?>>();

        var key = binding.Value!.Key;
        var values = new Dictionary<string, object?>(key.KeyFields.Count, StringComparer.Ordinal);
        foreach (var keyField in key.KeyFields)
        {
            if (keyField.ReferencedField is not { } referenced || !parentKeyValues.TryGetValue(referenced.Name, out var value))
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                    DefaultConfigurationProviderLog.ReferencedValueMissing(
                        _logger, key.KeyName, childContainer, parent.Name, keyField.ReferencedField?.Name ?? "(unresolved)"));
            values[keyField.LocalField.Name] = value;
        }

        DefaultConfigurationProviderLog.ForeignKeyFilled(
            _logger, key.KeyName, childContainer, parent.Name, Describe(values));
        return GenericResult<IReadOnlyDictionary<string, object?>>.Success(values);
    }

    /// <summary>The Foreign key by which rows of <paramref name="childContainer"/> hang from <paramref name="parent"/>.</summary>
    /// <remarks>
    /// It is the one key the parent's configuration lists among the keys referencing it
    /// (<see cref="IDataContainer.ReferencingKeys"/>) whose owner is the child's container and which
    /// references the parent's Physical key. A child joins its owner on the owner's Physical key; a key
    /// that references the parent's Logical key cites that record (a node's parent node, say) and does
    /// not hold the child. None, or more than one, is a failure: which key holds the owner is
    /// configuration, never a choice made here.
    /// </remarks>
    internal static IGenericResult<ReferencingKeyBinding> OwnerBinding(ILogger logger, IDataContainer parent, string childContainer)
    {
        var physical = parent.Keys
            .FirstOrDefault(k => string.Equals(k.KeyType.Name, "Physical", StringComparison.Ordinal) && k.KeyFields.Count > 0)?
            .KeyFields[0].LocalField.Name;
        if (physical is null)
            return GenericResult<ReferencingKeyBinding>.Failure(
                DefaultConfigurationProviderLog.PhysicalKeyNotDeclared(logger, parent.Name, childContainer));

        var referencing = parent.ReferencingKeys;
        if (referencing.IsFailure) return referencing.ToNewResult<ReferencingKeyBinding>();

        var bindings = referencing.Value!
            .Where(b => string.Equals(b.Owner.Name, childContainer, StringComparison.Ordinal)
                        && b.Key.KeyFields.Count > 0
                        && string.Equals(b.Key.KeyFields[0].ReferencedField?.Name, physical, StringComparison.Ordinal))
            .ToList();
        if (bindings.Count == 0)
            return GenericResult<ReferencingKeyBinding>.Failure(
                DefaultConfigurationProviderLog.NoForeignKeyToParent(logger, childContainer, parent.Name));
        if (bindings.Count > 1)
            return GenericResult<ReferencingKeyBinding>.Failure(
                DefaultConfigurationProviderLog.AmbiguousForeignKeyToParent(logger, childContainer, parent.Name, bindings.Count));

        return GenericResult<ReferencingKeyBinding>.Success(bindings[0]);
    }

    /// <summary>Field=value pairs for a trace line.</summary>
    internal static string Describe(IReadOnlyDictionary<string, object?> values)
        => string.Join(", ", values.Select(v => $"{v.Key}={v.Value}"));

    /// <summary>Reads inside the unit of work, on the connection holding its locks.</summary>
    /// <remarks>
    /// A record's own rows are read back mid-operation -- to find what a delete must retire, or
    /// whether a name already has a domain row. Reading those through the gateway instead would use a
    /// second connection, which blocks on the locks this unit is holding and waits until the command
    /// times out. Nothing is recorded for invalidation: a read changes nothing.
    /// </remarks>
    internal Task<IGenericResult<T>> Read<T>(IDataCommand command, DataStoreTarget target, CancellationToken ct)
        => _transaction.Execute<T>(command, target, ct);

    /// <summary>Runs one write whose row type is only known at runtime inside the unit of work.</summary>
    internal async Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken ct)
    {
        _written.Add(target);
        return await _transaction.Execute(command, target, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Commits every write in the unit, then drops the cached reads for the containers it changed.
    /// </summary>
    /// <remarks>
    /// Invalidating after the commit and not before: a rolled-back unit changed nothing, so dropping
    /// its cache entries would evict answers that are still true.
    /// </remarks>
    public async Task<IGenericResult> Commit(CancellationToken ct)
    {
        var committed = await _transaction.Commit(ct).ConfigureAwait(false);
        if (committed.IsFailure) return committed;

        for (var i = 0; i < _written.Count; i++)
            _gateway.InvalidateCachedResults(_written[i]);

        return GenericResult.Success();
    }

    /// <summary>Abandons every write in the unit.</summary>
    /// <remarks>
    /// The failure that caused the rollback is what the caller returns, so a rollback that itself
    /// fails is logged here rather than replacing it — losing the original would leave the caller
    /// reporting the cleanup instead of the cause.
    /// </remarks>
    public async Task Rollback(CancellationToken ct)
    {
        var rolledBack = await _transaction.Rollback(ct).ConfigureAwait(false);
        if (rolledBack.IsFailure)
            DefaultConfigurationProviderLog.WriteScopeRollbackFailed(_logger, rolledBack.CurrentMessage ?? string.Empty);
    }

    /// <summary>Disposes the transaction, which rolls it back if it was never committed.</summary>
    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
